using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Candle
{
	class Program
	{
		static void Main(string[] args)
		{
			var devices = Device.ListDevices();
			Console.WriteLine(String.Format("Found {0} devices", devices.Count));
			foreach (var device in devices)
			{
				Console.WriteLine("{0} : {1}", device.Path, device.State);
			}
		}
	}
}
