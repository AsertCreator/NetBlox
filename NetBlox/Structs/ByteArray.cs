using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Structs
{
	public struct ByteArray(byte[] data)
	{
		public byte[] Data = data;

		public override string ToString() => $"[ {Data.Length} {(Data.Length == 1 ? "byte" : "bytes")} ]";
	}
}
