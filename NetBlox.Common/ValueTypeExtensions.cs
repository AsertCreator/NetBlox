using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NetBlox.Common
{
	public static class ValueTypeExtensions
	{
		public static byte[] GetBytes<T>(T obj) // extension methods have betrayed me D:
		{
			int size = Marshal.SizeOf(obj);
			byte[] arr = new byte[size];

			IntPtr ptr = IntPtr.Zero;
			try
			{
				ptr = Marshal.AllocHGlobal(size);
				Marshal.StructureToPtr(obj, ptr, true);
				Marshal.Copy(ptr, arr, 0, size);
			}
			finally
			{
				Marshal.FreeHGlobal(ptr);
			}
			return arr;
		}
	}
}
