using Microsoft.Win32;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Windows;

namespace WindowsNBServerManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string? ServerRootFolder;
        public static List<Server> Servers = [];
        public static string TargetExecutable = "";
        public const string ServerExecutable = "NetBloxServer.exe";

        public App()
        {
            ReloadServerList();
        }
        public static void ReloadServerList()
        {
            var procs = Process.GetProcesses();

            Servers.Clear();

            for (int i = 0; i < procs.Length; i++)
            {
                if (procs[i].ProcessName == Path.GetFileNameWithoutExtension(ServerExecutable)) // thats so weird
                    Servers.Add(new Server(procs[i]));
            }
        }
        public static void ShutdownAllServers()
        {
            for (int i = 0; i < Servers.Count; i++)
                Servers[i].Shutdown();
        }
        public static Server SpawnServer(string piifile, string? placeoverride, string? univoverride, int maxplayers)
        {
            ProcessStartInfo pi = new();
            pi.WorkingDirectory = Path.GetDirectoryName(TargetExecutable);
            pi.UseShellExecute = true;
            pi.FileName = TargetExecutable;
            pi.Arguments = $"--rbxl \"{piifile}\" " + (placeoverride != null ? $"--placeor {placeoverride} " : "") + (univoverride != null ? $"--univor {univoverride} " : "") + $"--maxplayers {maxplayers}";
            Server srv = new(Process.Start(pi)!);
            Servers.Add(srv);
            return srv;
        }
    }
    public class Server
    {
        public string? PlaceName { get; set; }
        public string? UniverseName { get; set; }
        public string? AuthorName { get; set; }
        public string? RBXLPath { get; set; }
        public int PlaceId { get; set; }
        public int UniverseId { get; set; }
        public int AuthorId { get; set; }
        public int PlayerCount { get; set; }
        public int MaxPlayerCount { get; set; }
        public Process Process;

        public Server(Process p)
        {
            Process = p;

            using (NamedPipeClientStream cs = new(".", "netblox.index" + p.Id, PipeDirection.In)) 
            {
                cs.Connect();

                using (StreamReader sr = new StreamReader(cs))
                {
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
        }
        public void Shutdown() => Process.Kill();
    }
}
