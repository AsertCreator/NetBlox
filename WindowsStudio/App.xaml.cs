using NetBlox;
using Raylib_cs;
using System.Configuration;
using System.Data;
using System.Windows;
using Application = System.Windows.Application;

namespace NetBlox.Studio;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	public static GameManager? EditorGame;
	public static GameManager? ClientGame;
	public static GameManager? ServerGame;

	public App()
	{
		LogManager.LogInfo($"NetBlox Studio ({AppManager.VersionMajor}.{AppManager.VersionMinor}.{AppManager.VersionPatch}) is running...");

		Raylib.SetTraceLogLevel(TraceLogLevel.None);

		var v = Rlgl.GetVersion();
		if (v == GlVersion.OpenGl11 || v == GlVersion.OpenGl21)
		{
			System.Windows.Forms.MessageBox.Show("NetBlox cannot run on your device, because the OpenGL 3.3 isn't supported. Consider re-checking your system settings.");
			Environment.Exit(1);
		}
	}
}

