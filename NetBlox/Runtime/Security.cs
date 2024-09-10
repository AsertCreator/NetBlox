using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetBlox.Runtime
{
	public static class Security
	{
		public static int Level
		{
			get
			{
				if (impmutex) return implevel;
				object? lvl = TaskScheduler.CurrentJob.AssociatedObject1;
				if (lvl == null)
					return 0;
				return (int)lvl;
			}
		}
		private static bool impmutex = false;
		private static int implevel = 0;
		public static void Impersonate(int level)
		{
			while (impmutex) ;
			impmutex = true;
			implevel = level;
		}
		public static void EndImpersonate()
		{
			implevel = 0;
			impmutex = false;
		}
		public static void Require(string name, params Capability[] caps)
		{
			if (caps.Length == 0) return;
			if (!IsCompatible(Level, caps))
				throw new InvalidOperationException(name + " is not accessible (lacking capability " + caps[0] + ')'); // heh
		}
		public static bool IsCompatible(int level, params Capability[] cms)
		{
			switch (level)
			{
				case 1: /* None */ 
					return cms.Contains(Capability.None);
				case 2: /* GameScript */ 
					return cms.Contains(Capability.None);
				case 3: /* CoreScript */ 
					return cms.Contains(Capability.None) || cms.Contains(Capability.CoreSecurity);
				case 4: /* CommandBar */ 
					return cms.Contains(Capability.None) || cms.Contains(Capability.CoreSecurity) || cms.Contains(Capability.PluginSecurity);
				case 5: /* Plugin */     
					return cms.Contains(Capability.None) || cms.Contains(Capability.CoreSecurity) || cms.Contains(Capability.PluginSecurity);
				case 6: /* CorePlugin */ 
					return cms.Contains(Capability.None) || cms.Contains(Capability.CoreSecurity) || cms.Contains(Capability.PluginSecurity) || cms.Contains(Capability.RobloxScriptSecurity);
				case 7: /* StarterScript */ 
					return true;
				case 8: /* PublicServiceRemote */ 
					return true;
				case 9: /* Replicator */ 
					return cms.Contains(Capability.None) || cms.Contains(Capability.CoreSecurity) || cms.Contains(Capability.WritePlayerSecurity) || cms.Contains(Capability.RobloxScriptSecurity);
				default: return false;
			}
		}
		public enum Capability
		{
			None, CoreSecurity, PluginSecurity, WritePlayerSecurity, RobloxScriptSecurity, RobloxSecurity
		}
	}
}
