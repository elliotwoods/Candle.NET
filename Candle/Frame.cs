using System;
using System.Collections.Generic;
using System.Text;

namespace Candle
{
	public class Frame
	{
		public UInt32 ID;
		public byte[] data;
		public UInt32 timestamp;
		public bool Extended;
		public bool RTR;
		public bool Error;
	}
}
