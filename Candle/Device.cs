using System;
using System.Collections.Generic;
using System.Text;

namespace Candle
{
	public class Device
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
		List<Channel> FChannels = new List<Channel>();

		public Device(IntPtr deviceHandle)
		{
			this.FDeviceHandle = deviceHandle;
		}

		~Device()
		{
			NativeFunctions.candle_dev_free(this.FDeviceHandle);
		}

		public NativeFunctions.candle_devstate_t State
		{
			get
			{
				NativeFunctions.candle_devstate_t value;
				if(!NativeFunctions.candle_dev_get_state(this.FDeviceHandle, out value))
				{
					NativeFunctions.throwError(this.FDeviceHandle);
				}
				return value;
			}
		}

		public string Path
		{
			get
			{
				return NativeFunctions.candle_dev_get_path(this.FDeviceHandle);
			}
		}

		public void Open()
		{
			if(!NativeFunctions.candle_dev_open(this.FDeviceHandle))
			{
				NativeFunctions.throwError(this.FDeviceHandle);
			}

			uint timestamp;
			NativeFunctions.candle_dev_get_timestamp_us(this.FDeviceHandle, out timestamp);
			Console.WriteLine(timestamp);

			// List Channels
			this.FChannels.Clear();
			byte channelCount;
			if(!NativeFunctions.candle_channel_count(this.FDeviceHandle, out channelCount))
			{
				NativeFunctions.throwError(this.FDeviceHandle);
			}
			for(byte i=0; i<channelCount; i++)
			{
				//this.FChannels.Add(new Channel(this.FDeviceHandle, i));
			}
		}

		public void Close()
		{
			if (!NativeFunctions.candle_dev_close(this.FDeviceHandle))
			{
				NativeFunctions.throwError(this.FDeviceHandle);
			}
		}

		public UInt32 Timestamp
		{
			get
			{
				UInt32 value;
				if (!NativeFunctions.candle_dev_get_timestamp_us(this.FDeviceHandle, out value))
				{
					NativeFunctions.throwError(this.FDeviceHandle);
				}
				return value;
			}
		}

		public List<Channel> Channels
		{
			get
			{
				return this.FChannels;
			}
		}

	}
}
