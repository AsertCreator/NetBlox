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
	public App()
	{
		LogManager.LogInfo($"NetBlox DuoHost ({AppManager.VersionMajor}.{AppManager.VersionMinor}.{AppManager.VersionPatch}) is running...");

		Raylib.SetTraceLogLevel(TraceLogLevel.None);

		var v = Rlgl.GetVersion();
		if (v == GlVersion.OpenGl11 || v == GlVersion.OpenGl21)
		{
			System.Windows.Forms.MessageBox.Show("NetBlox cannot run on your device, because the OpenGL 3.3 isn't supported. Consider re-checking your system settings.");
			Environment.Exit(1);
		}

		AppManager.LoadFastFlags(Environment.GetCommandLineArgs());
	}
}

