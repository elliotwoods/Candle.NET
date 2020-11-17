using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Candle
{
	class Program
	{
		static void RunChannelTest(Channel channel)
		{
			var frame = new Frame();
			frame.Extended = true;
			frame.ID = 1 << 19;
			frame.data = new byte[]
			{
				1, 1, 0, 1, 0, 0, 0
			};

			Console.WriteLine("Sending");
			for(int i=0; i<100; i++)
			{
				channel.Send(frame);
				Console.Write(".");
			}
			
			Console.WriteLine("");
		}

		static void Main(string[] args)
		{
			var devices = Device.ListDevices();
			Console.WriteLine(String.Format("Found {0} devices", devices.Count));
			foreach (var device in devices)
			{
				Console.WriteLine("{0} : {1}", device.Path, device.State);

				device.Open();

				var channels = device.Channels;
				Console.WriteLine("Channel count: {0}", channels.Count);

				Console.WriteLine("Timestamp: {0}", device.Timestamp);

				foreach (var keyValue in channels)
				{
					Console.WriteLine("Channel {0} : ", keyValue.Key);

					var channel = keyValue.Value;

					var capabilities = channel.Capabilities;
					Console.WriteLine("\tCapabilities : ");

					foreach(var field in capabilities.GetType().GetFields())
					{
						Console.WriteLine("\t\t{0} : {1}", field.Name, field.GetValue(capabilities));
					}

					channel.Start();

					RunChannelTest(channel);


					channel.Stop();
				}

				var errors = device.ReceiveErrors();
				if(errors.Count > 0)
				{
					Console.WriteLine("Errors in device : ");
					foreach(var error in errors)
					{
						Console.WriteLine(error);
					}
				}

				device.Close();
			}
		}
	}
}
