using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox
{
	public class GameConfiguration
	{
		public string GameName = "";
		public bool SkipWindowCreation = false;
		public bool DoNotRenderAtAll = false;
		public bool ProhibitScripts = false;
		public bool ProhibitProcessing = false;
		public bool AsServer = false;
		public bool AsClient = false;
		public bool AsStudio = false;
		public ConfigFlags CustomFlags;
		public int VersionMargin = 0;
	}
}
