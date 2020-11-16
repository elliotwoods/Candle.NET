using System;
using System.Collections.Generic;
using System.Text;

namespace Candle
{
	public class Channel
	{
		IntPtr FDeviceHandle;
		byte FChannelIndex;

		public Channel(IntPtr deviceHandle, byte channelIndex)
		{
			this.FDeviceHandle = deviceHandle;
			this.FChannelIndex = channelIndex;
		}

		public NativeFunctions.candle_capability_t Capabilities
		{
			get
			{
				NativeFunctions.candle_capability_t capabilities;
				if (!NativeFunctions.candle_channel_get_capabilities(this.FDeviceHandle, this.FChannelIndex, out capabilities))
				{
					NativeFunctions.throwError(this.FDeviceHandle);
				}
				return capabilities;
			}
		}

		public void SetTiming(NativeFunctions.candle_bittiming_t value)
		{
			if (!NativeFunctions.candle_channel_set_timing(this.FDeviceHandle, this.FChannelIndex, ref value))
			{
				NativeFunctions.throwError(this.FDeviceHandle);
			}
		}

		public void SetBitrate(UInt32 value)
		{
			if (!NativeFunctions.candle_channel_set_bitrate(this.FDeviceHandle, this.FChannelIndex, value))
			{
				NativeFunctions.throwError(this.FDeviceHandle);
			}
		}

		public void Start()
		{
			if (!NativeFunctions.candle_channel_start(this.FDeviceHandle, this.FChannelIndex, 0))
			{
				NativeFunctions.throwError(this.FDeviceHandle);
			}
		}

		public void Stop()
		{
			if (!NativeFunctions.candle_channel_stop(this.FDeviceHandle, this.FChannelIndex))
			{
				NativeFunctions.throwError(this.FDeviceHandle);
			}
		}
	}
}
