using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Candle
{
	public class Device : IDisposable
	{
		public static List<Device> ListDevices()
		{
			var result = new List<Device>();

			IntPtr list;
			if(!NativeFunctions.candle_list_scan(out list))
			{
				throw (new Exception("Failed to get device list"));
			}

			byte count;
			if(!NativeFunctions.candle_list_length(list, out count))
			{
				throw (new Exception("Failed to get length of device list"));
			}

			for (byte i=0; i<count; i++)
			{
				IntPtr deviceHandle;
				if(NativeFunctions.candle_dev_get(list, i, out deviceHandle))
				{
					result.Add(new Device(deviceHandle));
				}
				else
				{
					throw (new Exception(String.Format("Failed to get device {0}", i)));
				}
			}

			if(!NativeFunctions.candle_list_free(list))
			{
				throw (new Exception(String.Format("Failed to free device list")));
			}

			return result;
		}

		IntPtr FDeviceHandle;
		Dictionary<int, Channel> FChannels = new Dictionary<int, Channel>();
		BlockingCollection<Action> FActionQueue;
		BlockingCollection<Exception> FExceptionQueue;
		Thread FThread = null;
		bool FIsClosing = false;

		public Device(IntPtr deviceHandle)
		{
			this.FDeviceHandle = deviceHandle;
		}

		public void Dispose()
		{
			this.Close();
			NativeFunctions.candle_dev_free(this.FDeviceHandle);
		}

		public NativeFunctions.candle_devstate_t State
		{
			get
			{
				var value = new NativeFunctions.candle_devstate_t();
				Action action = () =>
				{
					if(!NativeFunctions.candle_dev_get_state(this.FDeviceHandle, out value))
					{
						NativeFunctions.throwError(this.FDeviceHandle);
					}
				};

				if (this.IsOpen)
				{
					this.PerformBlocking(action);
				}
				else
				{
					action();
				}
				return value;
			}
		}

		public string Path
		{
			get
			{
				var path = new StringBuilder(255);
				Action action = () =>
				{
					if (!NativeFunctions.candle_dev_get_path(this.FDeviceHandle, path))
					{
						throw (new Exception("Failed to get path"));
					}
				};

				if(this.IsOpen)
				{
					this.PerformBlocking(action);
				}
				else
				{
					action();
				}
				
				return path.ToString();
			}
		}

		public UInt32 Timestamp
		{
			get
			{
				UInt32 value = 0;
				Action action = () =>
				{
					if (!NativeFunctions.candle_dev_get_timestamp_us(this.FDeviceHandle, out value))
					{
						NativeFunctions.throwError(this.FDeviceHandle);
					}
				};

				if (this.IsOpen)
				{
					this.PerformBlocking(action);
				}
				else
				{
					action();
				}

				return value;
			}
		}

		public void Open()
		{
			this.Close();

			if(!NativeFunctions.candle_dev_open(this.FDeviceHandle))
			{
				NativeFunctions.throwError(this.FDeviceHandle);
			}

			// Create queues
			this.FActionQueue = new BlockingCollection<Action>();
			this.FExceptionQueue = new BlockingCollection<Exception>();

			// Start device thread
			this.FIsClosing = false;
			this.FThread = new Thread(this.ThreadedUpdate);
			this.FThread.Name = "Candle";
			this.FThread.Start();

			// List Channels
			this.FChannels.Clear();
			byte channelCount;
			if(!NativeFunctions.candle_channel_count(this.FDeviceHandle, out channelCount))
			{
				NativeFunctions.throwError(this.FDeviceHandle);
			}
			for(byte i=0; i<channelCount; i++)
			{
				this.FChannels.Add(i, new Channel(this, i));
			}
		}

		public void Close()
		{
			if(this.FThread != null)
			{
				this.PerformBlocking(() =>
				{
					this.FIsClosing = true;
					if (!NativeFunctions.candle_dev_close(this.FDeviceHandle))
					{
						NativeFunctions.throwError(this.FDeviceHandle);
					}
				});

				// Close queues
				this.FActionQueue.CompleteAdding();
				this.FActionQueue.Dispose();
				this.FActionQueue = null;
				this.FExceptionQueue.CompleteAdding();
				this.FExceptionQueue.Dispose();
				this.FExceptionQueue = null;

				this.FThread.Join();
				this.FThread = null;
			}
		}

		public bool IsOpen
		{
			get
			{
				return this.FThread != null;
			}
		}

		public Dictionary<int, Channel> Channels
		{
			get
			{
				return this.FChannels;
			}
		}

		public IntPtr Handle
		{
			get
			{
				return this.FDeviceHandle;
			}
		}

		public void Perform(Action action)
		{
			this.FActionQueue.Add(action);
		}

		public void PerformBlocking(Action action)
		{
			var returnEvent = new ManualResetEvent(false);
			Exception exception = null;
			this.FActionQueue.Add(() =>
			{
				try
				{
					action();
				}
				catch(Exception e)
				{
					exception = e;
				}
				returnEvent.Set();
			});
			returnEvent.WaitOne();
			if(exception != null)
			{
				throw (exception);
			}
		}

		void ThreadedUpdate()
		{
			while(!this.FIsClosing)
			{
				try
				{
					// Rx frames
					{
						NativeFunctions.candle_frame_t nativeFrame;
						if (NativeFunctions.candle_frame_read(this.FDeviceHandle
							, out nativeFrame
							, 0))
						{
							// Find the channel 
							if (this.FChannels.ContainsKey(nativeFrame.channel)) {
								var frame = new Frame();

								var flags = (NativeFunctions.candle_id_flags)(nativeFrame.can_id);
								frame.Identifier = nativeFrame.can_id & ((1 << 29) - 1);
								frame.Extended = flags.HasFlag(NativeFunctions.candle_id_flags.CANDLE_ID_EXTENDED);
								frame.RTR = flags.HasFlag(NativeFunctions.candle_id_flags.CANDLE_ID_RTR);
								frame.Error = flags.HasFlag(NativeFunctions.candle_id_flags.CANDLE_ID_ERR);

								frame.Data = new byte[nativeFrame.can_dlc];
								Buffer.BlockCopy(nativeFrame.data, 0, frame.Data, 0, nativeFrame.can_dlc);

								frame.Timestamp = nativeFrame.timestamp_us;

								this.FChannels[nativeFrame.channel].NotifyReceive(frame);
							}
							else
							{
								throw (new Exception(String.Format("Cannot find channel {0}", nativeFrame.channel)));
							}
						}
					}

					// Perform actions
					{
						int count = 0;

						Action action;
						while (this.FActionQueue.TryTake(out action))
						{
							action();

							if(count++ > 64)
							{
								// We have trouble when sending more than 92 message in a row
								break;
							}
						}
					}
				}
				catch(Exception e)
				{
					Console.Write(e);
				}
			}
		}

		public List<Exception> ReceiveErrors()
		{
			var result = new List<Exception>();
			if (this.IsOpen)
			{
				Exception exception;
				while (this.FExceptionQueue.TryTake(out exception))
				{
					result.Add(exception);
				}
			}
			return result;
		}
	}
}
