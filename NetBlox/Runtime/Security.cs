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
				case 1: return true;
				case 2: return cms.Contains(Capability.None);
				case 3: return cms.Contains(Capability.None) || cms.Contains(Capability.CoreSecurity);
				case 4: return cms.Contains(Capability.None) || cms.Contains(Capability.CoreSecurity);
				case 5: return cms.Contains(Capability.None) || cms.Contains(Capability.CoreSecurity);
				case 6: return cms.Contains(Capability.None) || cms.Contains(Capability.CoreSecurity) || cms.Contains(Capability.RobloxScriptSecurity);
				case 7: return true;
				case 8: return true;
				case 9: return cms.Contains(Capability.None) || cms.Contains(Capability.CoreSecurity) || cms.Contains(Capability.RobloxScriptSecurity);
				default: return false;
			}
		}
		public enum Capability
		{
			None, CoreSecurity, WritePlayerSecurity, RobloxScriptSecurity, RobloxSecurity
		}
	}
}
