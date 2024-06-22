using NetBlox;
using Raylib_CsLo;
using System.Configuration;
using System.Data;
using System.Windows;
using Application = System.Windows.Application;

namespace WindowsStudio;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	public App()
	{
		LogManager.LogInfo($"NetBlox DuoHost ({AppManager.VersionMajor}.{AppManager.VersionMinor}.{AppManager.VersionPatch}) is running...");

		Raylib.SetTraceLogLevel((int)TraceLogLevel.LOG_NONE);

		var v = (rlGlVersion)RlGl.rlGetVersion();
		if (v == rlGlVersion.RL_OPENGL_11 || v == rlGlVersion.RL_OPENGL_21)
		{
			System.Windows.Forms.MessageBox.Show("NetBlox cannot run on your device, because the OpenGL 3.3 isn't supported. Consider re-checking your system settings.");
			Environment.Exit(1);
		}

		AppManager.LoadFastFlags(Environment.GetCommandLineArgs());
	}
}

