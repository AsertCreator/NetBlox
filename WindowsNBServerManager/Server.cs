using System.Diagnostics;
using System.IO.Pipes;

namespace NetBlox.SPSM
{
	public class Server
	{
		public string? PlaceName;
		public string? UniverseName;
		public string? AuthorName;
		public string? RBXLPath;
		public int PlaceId;
		public int UniverseId;
		public int AuthorId;
		public int PlayerCount;
		public int MaxPlayerCount;
		public Process Process;

		public Server(Process p)
		{
			Process = p;

			using (NamedPipeClientStream cs = new(".", "netblox.index" + p.Id, PipeDirection.In))
			{
				cs.Connect();
				using StreamReader sr = new StreamReader(cs);
				string[] s = sr.ReadToEnd().Split(Environment.NewLine);

				PlaceName = s[0];
				UniverseName = s[1];
				AuthorName = s[2];
				RBXLPath = s[3];
				PlaceId = int.Parse(s[4]);
				UniverseId = int.Parse(s[5]);
				AuthorId = int.Parse(s[6]);
				PlayerCount = int.Parse(s[7]);
				MaxPlayerCount = int.Parse(s[8]);
			}
		}
		public void ExecuteLua(string code)
		{
			using (NamedPipeClientStream cs = new(".", "netblox.rctrl" + Process.Id, PipeDirection.Out))
			{
				cs.Connect();
				using StreamWriter sr = new StreamWriter(cs);

				sr.WriteLine("runlua");
				sr.Write(code);
			}
		}
		public void Shutdown() => Process.Kill();
	}
}
