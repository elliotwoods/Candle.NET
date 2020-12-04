using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Candle
{
	public class Channel
	{
		Device FDevice;
		byte FChannelIndex;
		protected BlockingCollection<Frame> FRxQueue = new BlockingCollection<Frame>();

		protected DateTime FCounterLastTime;
		protected UInt64 FCounterRxBits = 0;
		protected UInt64 FCounterTxBits = 0;

		int FRxBitsPerSecond = 0;
		int FTxBitsPerSecond = 0;

		public Channel(Device device, byte channelIndex)
		{
			this.FDevice = device;
			this.FChannelIndex = channelIndex;
		}

		virtual public NativeFunctions.candle_capability_t Capabilities
		{
			get
			{
				var capabilities = new NativeFunctions.candle_capability_t();
				this.FDevice.PerformBlocking(() =>
				{
					if (!NativeFunctions.candle_channel_get_capabilities(this.FDevice.Handle, this.FChannelIndex, out capabilities))
					{
						NativeFunctions.throwError(this.FDevice.Handle);
					}
				});
				return capabilities;
			}
		}

		virtual public void SetTiming(NativeFunctions.candle_bittiming_t value)
		{
			this.FDevice.PerformBlocking(() =>
			{
				if (!NativeFunctions.candle_channel_set_timing(this.FDevice.Handle, this.FChannelIndex, ref value))
				{
					NativeFunctions.throwError(this.FDevice.Handle);
				}
			});
		}

		virtual public void SetBitrate(int value)
		{
			this.FDevice.PerformBlocking(() =>
			{
				if (!NativeFunctions.candle_channel_set_bitrate(this.FDevice.Handle, this.FChannelIndex, (UInt32) value))
				{
					NativeFunctions.throwError(this.FDevice.Handle);
				}
			});
		}

		virtual public void Start(int bitrate)
		{
			this.SetBitrate(bitrate);
			this.FDevice.PerformBlocking(() =>
			{
				if (!NativeFunctions.candle_channel_start(this.FDevice.Handle, this.FChannelIndex, 0))
				{
					NativeFunctions.throwError(this.FDevice.Handle);
				}
			});

			this.FCounterLastTime = DateTime.Now;
			this.FCounterRxBits = 0;
			this.FCounterTxBits = 0;
		}
		public void Update()
		{
			var now = DateTime.Now;
			var timeDelta = now - this.FCounterLastTime;

			var timeDeltaMillis = (UInt64)timeDelta.Milliseconds;
			if (timeDeltaMillis > 0)
			{
				this.FRxBitsPerSecond = (int)(this.FCounterRxBits * 1000 / timeDeltaMillis);
				this.FTxBitsPerSecond = (int)(this.FCounterTxBits * 1000 / timeDeltaMillis);
			}
			else
			{
				this.FRxBitsPerSecond = 0;
				this.FTxBitsPerSecond = 0;
			}

			this.FCounterLastTime = now;
			this.FCounterRxBits = 0;
			this.FCounterTxBits = 0;
		}

		virtual public void Stop()
		{
			this.FDevice.PerformBlocking(() =>
			{
				if (!NativeFunctions.candle_channel_stop(this.FDevice.Handle, this.FChannelIndex))
				{
					NativeFunctions.throwError(this.FDevice.Handle);
				}
			});
		}

		virtual public void Send(Frame frame, bool blocking = false)
		{
			this.FDevice.SendOnChannel(frame, this.FChannelIndex, blocking);
			
		}

		public void NotifyReceive(Frame frame)
		{
			this.FRxQueue.Add(frame);
			this.IncrementRx(frame.LengthOnBus);
		}

		public List<Frame> Receive()
		{
			var frames = new List<Frame>();
			Frame frame;
			while(this.FRxQueue.TryTake(out frame))
			{
				frames.Add(frame);
			}
			return frames;
		}

		public void IncrementTx(int bits)
		{
			this.FCounterTxBits += (UInt64)bits;
		}

		public void IncrementRx(int bits)
		{
			this.FCounterRxBits += (UInt64)bits;
		}

		public int RxBitsPerSecond
		{
			get
			{
				return this.FRxBitsPerSecond;
			}
		}

		public int TxBitsPerSecond
		{
			get
			{
				return this.FTxBitsPerSecond;
			}
		}

		public Device Device
		{
			get
			{
				return this.FDevice;
			}
		}

		public int Index
		{
			get
			{
				return this.FChannelIndex;
			}
		}
	}
}
