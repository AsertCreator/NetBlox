using Raylib_CsLo;

namespace NetBlox.Studio
{
	public static class Program
	{
		[STAThread]
		public static int Main(string[] args)
		{
			LogManager.LogInfo($"NetBlox Studio ({AppManager.VersionMajor}.{AppManager.VersionMinor}.{AppManager.VersionPatch}) is running...");
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			Raylib.SetTraceLogLevel((int)TraceLogLevel.LOG_NONE);
			var v = (rlGlVersion)RlGl.rlGetVersion();
			if (v == rlGlVersion.RL_OPENGL_11 || v == rlGlVersion.RL_OPENGL_21)
			{
				MessageBox.Show("NetBlox cannot run on your device, because the OpenGL 3.3 isn't supported. Consider re-checking your system settings.", 
					"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return 1;
			}

			AppManager.LoadFastFlags(args);
			Application.Run(new MainForm());
			return 0;
		}
	}
}
