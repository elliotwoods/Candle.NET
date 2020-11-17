using System;
using System.IO;
using System.Text;
using System.Threading;
using Candle;

// We perform the same tasks as in the NativeTest app example

namespace MirrorNativeApp
{
	class Program
	{
		const int deviceID = 1;

		static void sendMovementFrame(IntPtr device, byte channel, int position)
		{
			var frame = new NativeFunctions.candle_frame_t();
			{
				frame.can_id = (deviceID << 19) | (UInt32) NativeFunctions.candle_id_flags.CANDLE_ID_EXTENDED;
				frame.can_dlc = 7;
				frame.flags = 0;
				frame.data = new byte[8];

				var binaryWriter = new BinaryWriter(new MemoryStream(frame.data));
			
				// Write to TargetPosition register (for Muscle Memory test)
				binaryWriter.Write((byte)1);
				binaryWriter.Write((UInt16)12);
				binaryWriter.Write((Int32) position * 1000);
			}

			if (!NativeFunctions.candle_frame_send(device, channel, ref frame))
			{
				Console.Write("Failed to send CAN frame");
			}

			if(NativeFunctions.candle_frame_read(device, out frame, 0))
			{
				Console.Write('R');
			}
		}

		static void sendFrames(IntPtr device, byte channel)
		{
			var frame = new NativeFunctions.candle_frame_t();
			{
				frame.can_id = (deviceID << 19) | (UInt32)NativeFunctions.candle_id_flags.CANDLE_ID_EXTENDED;
				frame.can_dlc = 7;
				frame.flags = 0;
				frame.data = new byte[] {
					1,
					1,
					0,
					1,
					0,
					0,
					0,
					0,
				};
			}


			Console.WriteLine("Sending init frame : ");
			if (!NativeFunctions.candle_frame_send(device, channel, ref frame))
			{
				Console.WriteLine("Failed to send CAN frame");
			}

			Console.WriteLine("Sending moves : ");
			for (int i = 0; i < 100; i++)
			{
				sendMovementFrame(device, channel, i);
				Console.Write(".");
				Thread.Sleep(1);
			}
			for (int i = 100; i >= 0; i--)
			{
				sendMovementFrame(device, channel, i);
				Console.Write(".");
				Thread.Sleep(1);
			}

			Console.WriteLine("Receiving all : ");
			int count = 0;
			while (NativeFunctions.candle_frame_read(device, out frame, 100))
			{
				var id = frame.can_id;

				if ((id & (UInt32)NativeFunctions.candle_id_flags.CANDLE_ID_EXTENDED) > 0)
				{
					Console.Write("E, ");
				}
				if ((id & (UInt32)NativeFunctions.candle_id_flags.CANDLE_ID_RTR) > 0)
				{
					Console.Write("R, ");
				}
				if ((id & (UInt32)NativeFunctions.candle_id_flags.CANDLE_ID_ERR) > 0)
					{
					Console.Write("ERR, ");
				}

				Console.WriteLine("ID : {0}, DLC : {1}, Data : {2:X},{3:X},{4:X},{5:X},{6:X},{7:X},{8:X},{9:X}, Time : {9}"
					, id
					, frame.can_dlc
					, frame.data[0]
					, frame.data[1]
					, frame.data[2]
					, frame.data[3]
					, frame.data[4]
					, frame.data[5]
					, frame.data[6]
					, frame.data[7]
					, frame.timestamp_us / 1000
				);

				if(count++ > 100)
				{
					break;
				}

			}
		}

		static void runChannel(IntPtr device, byte channel)
		{
			NativeFunctions.candle_capability_t capabilities;
			if (!NativeFunctions.candle_channel_get_capabilities(device, channel, out capabilities))
			{
				Console.WriteLine("Failed to get capabilities");
			}
			else
			{
				Console.WriteLine("Capabilities: ");
				Console.WriteLine("\t feature: {0}", capabilities.feature);
				Console.WriteLine("\t fclk_can: {0}", capabilities.fclk_can);
				Console.WriteLine("\t tseg1_min: {0}", capabilities.tseg1_min);
				Console.WriteLine("\t tseg1_max: {0}", capabilities.tseg1_max);
				Console.WriteLine("\t tseg2_min: {0}", capabilities.tseg2_min);
				Console.WriteLine("\t tseg2_max: {0}", capabilities.tseg2_max);
				Console.WriteLine("\t sjw_max: {0}", capabilities.sjw_max);
				Console.WriteLine("\t brp_min: {0}", capabilities.brp_min);
				Console.WriteLine("\t brp_max: {0}", capabilities.brp_max);
				Console.WriteLine("\t brp_inc: {0}", capabilities.brp_inc);
			}

			if (!NativeFunctions.candle_channel_set_bitrate(device, channel, 500000))
			{
				Console.WriteLine("Failed to set bit rate");
			}

			if (!NativeFunctions.candle_channel_start(device, channel, 0))
			{
				Console.WriteLine("Failed to start channel");
			}

			sendFrames(device, channel);

			if (!NativeFunctions.candle_channel_stop(device, channel))
			{
				Console.WriteLine("Failed to set top channel");
			}
		}

		static void runDevice(IntPtr device)
		{
			Console.WriteLine("Opening device");
			if (!NativeFunctions.candle_dev_open(device))
			{
				Console.WriteLine("Failed to open device");
				return;
			}

			UInt32 timestamp;
			if (!NativeFunctions.candle_dev_get_timestamp_us(device, out timestamp))
			{
				Console.WriteLine("Failed to open device");
			}
			else
			{
				Console.WriteLine("Timestamp : {0}", timestamp);
			}

			byte numChannels;
			if (!NativeFunctions.candle_channel_count(device, out numChannels))
			{
				Console.WriteLine("Failed to get number of channels");
			}
			else
			{
				Console.WriteLine("Channel count : {0}", numChannels);
			}

			for (byte channel = 0; channel < numChannels; channel++)
			{
				runChannel(device, channel);
			}

			Console.WriteLine("Closing device");
			if (!NativeFunctions.candle_dev_close(device))
			{
				Console.WriteLine("Failed to close device ");
			}
		}

		static void Main(string[] args)
		{
			// Print CAN devicesfa

			IntPtr deviceList;
			if (!NativeFunctions.candle_list_scan(out deviceList))
			{
				Console.WriteLine("Failed to get CAN devices");
			}

			byte count;
			if (!NativeFunctions.candle_list_length(deviceList, out count))
			{
				Console.WriteLine("Failed to get list length");
			}
			Console.WriteLine("Found {0} devices", count);

			// Run CAN devices
			for (byte i = 0; i < count; i++)
			{
				IntPtr device;
				if (!NativeFunctions.candle_dev_get(deviceList, i, out device))
				{
					Console.WriteLine("Failed to get device {0}", i);
					continue;
				}

				NativeFunctions.candle_devstate_t deviceState;
				if (!NativeFunctions.candle_dev_get_state(device, out deviceState))
				{
					Console.WriteLine("Failed to get device state ");
				}

				Console.WriteLine("Device state : {0}", deviceState.ToString());

				var path = new StringBuilder(255);
				if (!NativeFunctions.candle_dev_get_path(device, path))
				{
					Console.WriteLine("Failed to get device path ");
				}
				else
				{
					Console.WriteLine("Device {0} path : {1}", i, path);
				}

				runDevice(device);

				NativeFunctions.candle_dev_free(device);
			}

			NativeFunctions.candle_list_free(deviceList);
		}
	}
}
