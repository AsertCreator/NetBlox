using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetBlox.Common
{
	public static class StreamExtensions
	{
		public static string ReadToEnd(this Stream stream)
		{
			using StreamReader sr = new(stream);
			return sr.ReadToEnd();
		}
	}
}
