using System;
using System.Threading;
using Candle;

namespace TestApp
{
	class Program
	{
		static void Main(string[] args)
		{
			var devices = Device.ListDevices();

			foreach (var device in devices)
			{
				device.Open();
				foreach(var keyValue in device.Channels) {
					var channel = keyValue.Value;
					channel.Start(500000);

					// Send frame
					{
						var frame = new Frame();
						frame.Identifier = 1 << 19;
						frame.Extended = true;
						frame.Data = new byte[3] { 0, 0, 0};
						channel.Send(frame);
					}

					Thread.Sleep(100);

					// Receive frames
					var receivedFrames = channel.Receive();
					foreach (var frame in receivedFrames)
					{
						Console.WriteLine(frame);
					}

					channel.Stop();
				}

				var errors = device.ReceiveErrors();
				foreach (var error in errors)
				{
					Console.WriteLine(error);
				}

				device.Close();
			}
		}
	}
}
