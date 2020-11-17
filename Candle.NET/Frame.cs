using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Candle
{
	public class Frame
	{
		public UInt32 ID;
		public byte[] Data;
		public UInt32 Timestamp;
		public bool Extended;
		public bool RTR;
		public bool Error;

		public override string ToString()
		{
			var value = String.Format("ID : {0}, Data : {1}, Time : {2}us"
				, this.ID
				, BitConverter.ToString(this.Data)
				, this.Timestamp
			);

			if(this.Extended)
			{
				value += " EXT";
			}
			if (this.RTR)
			{
				value += " RTR";
			}
			if (this.Error)
			{
				value += " Error";
			}

			return value;
		}
	}
}
