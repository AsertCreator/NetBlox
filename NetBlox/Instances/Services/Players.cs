using NetBlox.Runtime;

namespace NetBlox.Instances.Services
{
	public class Players : Instance
	{
		[Lua([Security.Capability.None])]
		public Instance? LocalPlayer => CurrentPlayer;
		public Player? CurrentPlayer;

		public Players(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Players) == classname) return true;
			return base.IsA(classname);
		}
		[Lua([Security.Capability.CoreSecurity])]
		public Player CreateNewPlayer(string name, bool local)
		{
			Player player = new(GameManager)
			{
				Name = name,
				Parent = this,
				IsLocalPlayer = local
			};

			return player;
		}
		[Lua([Security.Capability.CoreSecurity])]
		public void KickAll(string msg)
		{
			for (int i = 0; i < Children.Count; i++)
			{
				var ch = Children[i];
				if (ch is Player)
					(ch as Player)!.Kick(msg);
			}
		}
	}
}
