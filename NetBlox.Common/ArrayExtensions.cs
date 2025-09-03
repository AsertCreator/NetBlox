using System.Diagnostics;

namespace NetBlox
{
	public static class ArrayExtensions
	{
		public static void ForEach<T>(this T[] array, Action<T> action)
		{
			for (int i = 0; i < array.Length; i++)
				action(array[i]);
		}
	}
}
