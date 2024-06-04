using Version = NetBlox.Common.Version;

namespace NetBlox.SPSM
{
    public static class Program
    {
		public static void Main(string[] args)
		{
			Console.WriteLine($"NetBlox Server Manager & Public Service, ({Version.VersionMajor}.{Version.VersionMinor}.{Version.VersionPatch})");
			while (true)
			{
				Console.Write(">> ");
				string[] words = Console.ReadLine().Split(' ');
				switch (words[0])
				{
					case "help":
						Console.WriteLine("help   - show help menu");
						Console.WriteLine("create - creates a new server");
						break;
					case "create":

						break;
				}
			}
		}
	}
}
