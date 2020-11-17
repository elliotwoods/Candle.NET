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
		BlockingCollection<Frame> FRxQueue = new BlockingCollection<Frame>();

		public Channel(Device device, byte channelIndex)
		{
			this.FDevice = device;
			this.FChannelIndex = channelIndex;
		}

		public NativeFunctions.candle_capability_t Capabilities
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

		public void SetTiming(NativeFunctions.candle_bittiming_t value)
		{
			this.FDevice.PerformBlocking(() =>
			{
				if (!NativeFunctions.candle_channel_set_timing(this.FDevice.Handle, this.FChannelIndex, ref value))
				{
					NativeFunctions.throwError(this.FDevice.Handle);
				}
			});
		}

		public void SetBitrate(UInt32 value)
		{
			this.FDevice.PerformBlocking(() =>
			{
				if (!NativeFunctions.candle_channel_set_bitrate(this.FDevice.Handle, this.FChannelIndex, value))
				{
					NativeFunctions.throwError(this.FDevice.Handle);
				}
			});
		}

		public void Start()
		{
			this.FDevice.PerformBlocking(() =>
			{
				if (!NativeFunctions.candle_channel_start(this.FDevice.Handle, this.FChannelIndex, 0))
				{
					NativeFunctions.throwError(this.FDevice.Handle);
				}
			});
		}

		public void Stop()
		{
			this.FDevice.PerformBlocking(() =>
			{
				if (!NativeFunctions.candle_channel_stop(this.FDevice.Handle, this.FChannelIndex))
				{
					NativeFunctions.throwError(this.FDevice.Handle);
				}
			});
		}

		public void Send(Frame frame)
		{
			var nativeFrame = new NativeFunctions.candle_frame_t();
			nativeFrame.can_id = frame.ID;
			if(frame.Extended)
			{
				nativeFrame.can_id |= (UInt32) NativeFunctions.candle_id_flags.CANDLE_ID_EXTENDED;
			}
			if (frame.RTR)
			{
				nativeFrame.can_id |= (UInt32)NativeFunctions.candle_id_flags.CANDLE_ID_RTR;
			}
			if (frame.Error)
			{
				nativeFrame.can_id |= (UInt32)NativeFunctions.candle_id_flags.CANDLE_ID_ERR;
			}

			nativeFrame.data = new byte[8];
			nativeFrame.can_dlc = (byte) frame.data.Length;
			Buffer.BlockCopy(frame.data, 0, nativeFrame.data, 0, frame.data.Length);

			this.FDevice.Perform(() =>
			{
				if (!NativeFunctions.candle_frame_send(this.FDevice.Handle, this.FChannelIndex, ref nativeFrame))
				{
					NativeFunctions.throwError(this.FDevice.Handle);
				}
			});
		}

		public void NotifyReceive(Frame frame)
		{
			this.FRxQueue.Add(frame);
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
	}
}
