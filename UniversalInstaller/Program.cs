using Microsoft.Win32;
using NetBlox.Common;
using System;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UniversalInstaller
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			if (OperatingSystem.IsWindows())
			{
				Console.WriteLine("[+] Running on Windows, determining whether NetBlox is installed...");
				if (File.Exists("./NetBloxClient.exe"))
				{
					Console.WriteLine("[+] NetBlox is installed, checking its version...");
					var proc = new Process()
					{
						StartInfo = new ProcessStartInfo()
						{
							FileName = Path.GetFullPath("./NetBloxClient.exe"),
							Arguments = "check",
							RedirectStandardOutput = true,
							CreateNoWindow = true
						}
					};
					var ver = "";
					proc.OutputDataReceived += (x, y) =>
					{
						string target = y.Data ?? "";
						ver = target.Substring(target.IndexOf('('), target.IndexOf('(') - target.IndexOf(')'));
					};
					proc.Start();
					if (!proc.WaitForExit(10000))
					{
						proc.Kill();
						Console.WriteLine("[+] NetBlox Client does not respond, downloading latest...");
						await DownloadLatest();
					}
					while (ver == "") ;
					GetLatestVersion(ver).Wait();
				}
				else
				{
					Console.WriteLine("[+] No NetBlox is installed, downloading it...");
					DownloadLatest().Wait();
				}
			}
			else
			{
				Console.WriteLine("[-] Bootstrapper does not currently support non-Windows systems...");
				Environment.Exit(1);
			}
		}
		static async Task<string> GetLatestVersion(string ver)
		{
			try
			{
				var hc = new HttpClient();
				var req = new HttpRequestMessage()
				{
					RequestUri = new Uri("https://raw.githubusercontent.com/AsertCreator/NetBlox/master/NetBlox.Common/Version.cs")
				};
				req.Headers.Add("User-Agent", "NetBloxInstallerv" +
					NetBlox.Common.Version.VersionMajor + "." + NetBlox.Common.Version.VersionMinor + "." + NetBlox.Common.Version.VersionPatch);

				var data = await ((await hc.SendAsync(req)).Content.ReadAsStringAsync());

				Console.WriteLine("Code: " + data);

				return data;
			}
			catch
			{
				Console.WriteLine($"[-] Unable to request GitHub for latest version, exiting...");
				Environment.Exit(1);
				return "";
			}
		}
		static async Task DownloadLatest()
		{
			try
			{
				var hc = new HttpClient();
				var req = new HttpRequestMessage()
				{
					RequestUri = new Uri("https://api.github.com/repos/AsertCreator/NetBlox/actions/artifacts")
				};
				req.Headers.Add("User-Agent", "NetBloxInstallerv" +
					NetBlox.Common.Version.VersionMajor + "." + NetBlox.Common.Version.VersionMinor + "." + NetBlox.Common.Version.VersionPatch);
				req.Headers.Add("Accept", "application/vnd.github+json");
				req.Headers.Add("Authorization", "Bearer " + MathE.Roll("fhuitc^q`u^00@LYW5KX1RBf[iGoPFOV4^dbOBG73VKrh397jF{V5yOS@bWhV1bx4b2J8Ut9GoUhWJHTXKNESQ`63pn7i", 1)); // it doesn't have any scopes.
				req.Headers.Add("X-GitHub-Api-Version", "2022-11-28");

				var data = await ((await hc.SendAsync(req)).Content.ReadAsStringAsync());
				var json = JsonDocument.Parse(data);

				try
				{
					var artifacts = json.RootElement.GetProperty("artifacts");
					var latest = artifacts[1].GetProperty("archive_download_url").GetString() ?? "";

					Console.WriteLine($"[+] Downloading from {latest}...");
					req = new HttpRequestMessage()
					{
						RequestUri = new Uri(latest)
					};
					req.Headers.Add("User-Agent", "NetBloxInstallerv" +
						NetBlox.Common.Version.VersionMajor + "." + NetBlox.Common.Version.VersionMinor + "." + NetBlox.Common.Version.VersionPatch);
					req.Headers.Add("Accept", "application/vnd.github+json");
					req.Headers.Add("Authorization", "Bearer " + MathE.Roll("fhuitc^q`u^00@LYW5KX1RBf[iGoPFOV4^dbOBG73VKrh397jF{V5yOS@bWhV1bx4b2J8Ut9GoUhWJHTXKNESQ`63pn7i", 1));
					req.Headers.Add("X-GitHub-Api-Version", "2022-11-28");

					var str = (await hc.SendAsync(req)).Content.ReadAsStream();
					ZipArchive za = new(str);
					za.ExtractToDirectory(".", true);
					Console.WriteLine($"[+] Downloaded successfully!");
					// we set some registry thingies

					var rk = Registry.ClassesRoot.CreateSubKey("netblox-client"); // shut up vs we wont get here anyway
					rk.SetValue(null, "URL:NetBlox");
					rk.SetValue("URL Protocol", "");
					rk.CreateSubKey("shell").CreateSubKey("open").CreateSubKey("command").SetValue(null, Path.GetFullPath("NetBloxClient.exe"));
				}
				catch (Exception ex)
				{
					Console.WriteLine($"[-] Unable to download latest version, though link is known, exception: {ex}, msg: {ex.Message}, exiting...");
					Environment.Exit(1);
				}
			}
			catch
			{
				Console.WriteLine($"[-] Unable to request GitHub for latest version, exiting...");
				Environment.Exit(1);
			}
		}
	}
}
