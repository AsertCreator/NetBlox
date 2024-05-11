using NetBlox.Runtime;

namespace NetBlox.Instances.Services
{
	public class Players : Instance
	{
		[Lua([Security.Capability.None])]
		public Instance? LocalPlayer { get; set; }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Players) == classname) return true;
			return base.IsA(classname);
		}
		[Lua([Security.Capability.RobloxScriptSecurity])]
		public Player CreateNewPlayer(string name, bool local)
		{
			Player player = new()
			{
				Name = name,
				Parent = this,
				IsLocalPlayer = local
			};

			return player;
		}
	}
}
