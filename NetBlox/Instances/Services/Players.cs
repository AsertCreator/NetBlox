using NetBlox.Runtime;
using System.Diagnostics;

namespace NetBlox.Instances.Services
{
	[Service]
	public class Players : Instance
	{
		[Lua([Security.Capability.None])]
		public Instance? LocalPlayer => CurrentPlayer;
		public Player? CurrentPlayer;

		public Players(GameManager ins) : base(ins)
		{
			GameManager.RegisterService(this, ServiceType.Players);
		}

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Players) == classname) return true;
			return base.IsA(classname);
		}
		[Lua([Security.Capability.None])]
		public Player? GetPlayerFromCharacter(Instance inst)
		{
			var children = Children.ToArray();
			for (int i = 0; i < children.Length; i++)
			{
				var player = children[i] as Player;
				Debug.Assert(player != null);
				if (player.Character == inst)
					return player;
			}
			return null;
		}
		[Lua([Security.Capability.CoreSecurity])]
		public Player CreateNewPlayer(string name, bool local)
		{
			Security.Impersonate(8);
			Player player = new(GameManager)
			{
				Name = name,
				Parent = this,
				IsLocalPlayer = local
			};
			Security.EndImpersonate();

			return player;
		}
		[Lua([Security.Capability.CoreSecurity])]
		public Player CreateApplicationPlayer()
		{
			Security.Impersonate(8);
			Player player = new(GameManager)
			{
				Name = GameManager.Username,
				Parent = this,
				IsLocalPlayer = true
			};
			player.SetUserId(GameManager.CurrentProfile.UserId);
			CurrentPlayer = player;
			Security.EndImpersonate();

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
