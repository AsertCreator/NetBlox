namespace NetBlox.Runtime
{
	public static class Security
	{
		public static int Level
		{
			get
			{
				if (implevels.Count > 0) return implevels.Peek();
				object? lvl = TaskScheduler.CurrentJob.SecurityLevel;
				if (lvl == null)
					return 0;
				return (int)lvl;
			}
		}
		private static Stack<int> implevels = [];

		public static void Impersonate(int level) => implevels.Push(level);
		public static void EndImpersonate() => implevels.Pop();

		public static void Require(string name, params Capability[] caps)
		{
			if (caps.Length == 0) return;
			if (!IsCompatible(Level, caps))
				throw new InvalidOperationException(name + " is not accessible (lacking capability " + caps[0] + ')'); // heh
		}
		public static bool IsCompatible(int level, params Capability[] cms) => level switch
		{
			/* None */
			1 => cms.Contains(Capability.None),
			/* GameScript */
			2 => cms.Contains(Capability.None),
			/* CoreScript */
			3 => cms.Contains(Capability.None) || cms.Contains(Capability.CoreSecurity),
			/* CommandBar */
			4 => cms.Contains(Capability.None) || cms.Contains(Capability.CoreSecurity) || cms.Contains(Capability.PluginSecurity),
			/* Plugin */
			5 => cms.Contains(Capability.None) || cms.Contains(Capability.CoreSecurity) || cms.Contains(Capability.PluginSecurity),
			/* CorePlugin */
			6 => cms.Contains(Capability.None) || cms.Contains(Capability.CoreSecurity) || cms.Contains(Capability.PluginSecurity) || cms.Contains(Capability.RobloxScriptSecurity),
			/* StarterScript */
			7 => true,
			/* PublicServiceRemote */
			8 => true,
			/* Replicator */
			9 => cms.Contains(Capability.None) || cms.Contains(Capability.CoreSecurity) || cms.Contains(Capability.WritePlayerSecurity) || cms.Contains(Capability.RobloxScriptSecurity),
			_ => false,
		};
		public enum Capability
		{
			None, CoreSecurity, PluginSecurity, WritePlayerSecurity, RobloxScriptSecurity, RobloxSecurity
		}
	}
}
