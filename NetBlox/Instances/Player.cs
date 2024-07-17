using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Instances;
using System.Text.Json.Serialization;
using NetBlox.Structs;
using System.Text;

namespace NetBlox.Instances
{
	[Creatable]
	public class Player : Instance
	{
		[Lua([Security.Capability.None])]
		public Instance? Character { get; set; }
		[Lua([Security.Capability.None])]
		public Instance? RespawnLocation { get; set; }
		[Lua([Security.Capability.None])]
		public bool Guest { get; set; }
		[Lua([Security.Capability.None])]
		public long UserId => userId;
		[Lua([Security.Capability.None])]
		public long AccountAge => age;
		[Lua([Security.Capability.None])]
		public double CameraMaxZoomDistance { get; set; } = 32;
		[Lua([Security.Capability.None])]
		public double CameraMinZoomDistance { get; set; } = 0.2;
		[Lua([Security.Capability.None])]
		public bool AutoJumpEnabled { get; set; } = true;
		[Lua([Security.Capability.None])]
		public long CharacterAppearanceId { get; set; }

		public bool IsLocalPlayer = false;
		public RemoteClient? Client;
		public long userId;
		public long age;

		public Player(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public void SaveNumber(string key, int num) => 
			GameManager.CurrentProfile.SetPlayerDataAsync(key.GetHashCode(), BitConverter.GetBytes(num)).ConfigureAwait(false);
		[Lua([Security.Capability.None])]
		public void SaveString(string key, string data) =>
			GameManager.CurrentProfile.SetPlayerDataAsync(key.GetHashCode(), Encoding.UTF8.GetBytes(data)).ConfigureAwait(false);
		[Lua([Security.Capability.CoreSecurity])]
		public void SetUserId(long userid) => userId = userid;
		[Lua([Security.Capability.CoreSecurity])]
		public void SetAccountAge(long age) => this.age = age;
		[Lua([Security.Capability.CoreSecurity])]
		public void Reload()
		{
			ClearAllChildren();
			Backpack bc = new(GameManager);
			PlayerGui pg = new(GameManager);
			bc.Parent = this;
			pg.Parent = this;

			var sg = Root.GetService<StarterGui>().GetChildren();
			for (int i = 0; i < sg.Length; i++)
			{
				var cl = sg[i].Clone();
				if (cl == null) return;
				cl.Parent = pg;
			}

			var sp = Root.GetService<StarterPack>().GetChildren();
			for (int i = 0; i < sp.Length; i++)
			{
				var cl = sp[i].Clone();
				if (cl == null) return;
				cl.Parent = bc;
			}

			LogManager.LogInfo("Reloaded " + Name + "'s backpack and GUI!");
		}
		[Lua([Security.Capability.None])]
		public void LoadCharacterOld()
		{
			if (!GameManager.NetworkManager.IsServer && false)
				throw new Exception("Cannot call LoadCharacter from client!");

			var ch = new Character(GameManager);
			var face = new Decal(GameManager);
			var workspace = Root.GetService<Workspace>();

			if (Character != null)
				Character.Destroy();

			ch.Name = Name;
			ch.Color3 = GetPlayerColor().Color;
			ch.IsLocalPlayer = true;
			ch.Anchored = true;
			ch.Parent = workspace;
			face.Texture = "rbxasset://textures/smile.png";
			face.Face = Faces.Front;
			face.Parent = ch;

			if (workspace.MainCamera != null)
				(workspace.MainCamera as Camera)!.CameraSubject = ch;

			Character = ch;
		}
		[Lua([Security.Capability.CoreSecurity])]
		public void LoadCharacter()
		{
			var chmodel = new Model(GameManager);
			var humanoid = new Humanoid(GameManager);

			chmodel.Name = Name;
			chmodel.Parent = Root.GetService<Workspace>();
			humanoid.Parent = chmodel;


		}
		public BrickColor GetPlayerColor()
		{
			int idx = (int)(Math.Abs(CharacterAppearanceId) % 100);
			BrickColor? bc = BrickColor.ByIndex(idx);
			while (!bc.HasValue)
				bc = BrickColor.ByIndex(++idx);
			return bc.Value;
		}
		[Lua([Security.Capability.None])]
		public void Kick(string msg) => GameManager.NetworkManager.PerformKick(Client, msg, IsLocalPlayer);
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Player) == classname) return true;
			return base.IsA(classname);
		}
		[Lua([Security.Capability.None])]
		public override void Destroy() // also destroy character
		{
			base.Destroy();
			Character?.Destroy();
			if (IsLocalPlayer || GameManager.NetworkManager.IsServer)
				Kick("Player has been removed from this DataModel");
		}
	}
}
