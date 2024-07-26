using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetBlox.Runtime
{
	public static class Security
	{
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
					return cms.Contains(Capability.None) || cms.Contains(Capability.CoreSecurity) || cms.Contains(Capability.RobloxScriptSecurity);
				default: return false;
			}
		}
		public enum Capability
		{
			None, CoreSecurity, PluginSecurity, WritePlayerSecurity, RobloxScriptSecurity, RobloxSecurity
		}
	}
}
