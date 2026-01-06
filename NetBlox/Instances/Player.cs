using MoonSharp.Interpreter;
using NetBlox.Instances.Services;
using NetBlox.Network;
using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;
using System.Numerics;
using System.Text;

namespace NetBlox.Instances
{
	[Creatable]
	[ImpersonateDuringReplication(Level = 8)]
	public class Player : Instance
	{
		[Lua([Security.Capability.None])]
		public Instance? Character 
		{ 
			get => character; 
			set
			{
				if (value == null)
					return;

				var humanoid = value.FindFirstChild("Humanoid") as Humanoid;
				if (humanoid == null)
					return;

				if (character != null)
				{
					var oldhumanoid = character.FindFirstChild("Humanoid") as Humanoid;
					if (oldhumanoid != null)
						oldhumanoid.IsLocalPlayer = false;
				}

				character = value;

				if (GameManager.NetworkManager.IsServer)
				{
					Client.WaitForInstanceArrival(humanoid, () =>
					{
						// we're "hopefully" guaranteed that character's model had already replicated, so
						// it technically qualifies as a working humanoid
						character.GetDescendantsOfType<BasePart>().ForEach(x => x.SetNetworkOwner(this));
						Client.SendPacket(NPSetPlayableCharacter.Create(character as Model));
					});
				}
				else
				{
					if (!IsLocalPlayer)
						return;

					humanoid.IsLocalPlayer = true;

					var camera = GameManager.RenderManager.CurrentCamera;
					camera.CameraSubject = humanoid;
				}
			}
		}
		[Lua([Security.Capability.None])]
		public Instance? RespawnLocation { get; set; }
		[Lua([Security.Capability.None])]
		public bool Guest => userId < 0;
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
		public override string Name
		{
			get => base.Name;
			set
			{
				Security.Require("Renaming a Player", Security.Capability.WritePlayerSecurity);
				base.Name = value;
			}
		}

		public bool WasKicked = false;
		public Instance? character;
		public bool IsLocalPlayer = false;
		public RemoteClient? Client;
		public long userId;
		public long age;

		public Player(GameManager ins) : base(ins)
		{
			Security.Require("Creating a Player", Security.Capability.WritePlayerSecurity);
		}

		[Lua([Security.Capability.None])]
		public void SaveNumber(string key, int num)
		{
			if (!IsLocalPlayer)
				throw new Exception("Cannot call Save-/Load- Player APIs on other players!");
			GameManager.CurrentProfile.SetPlayerDataAsync(key.GetHashCode(), BitConverter.GetBytes(num)).ConfigureAwait(false);
		}
		[Lua([Security.Capability.None])]
		public void SaveString(string key, string data)
		{
			if (!IsLocalPlayer)
				throw new Exception("Cannot call Save-/Load- Player APIs on other players!");
			GameManager.CurrentProfile.SetPlayerDataAsync(key.GetHashCode(), Encoding.UTF8.GetBytes(data)).ConfigureAwait(false);
		}
		[Lua([Security.Capability.None])]
		public void SaveBool(string key, bool data)
		{
			if (!IsLocalPlayer)
				throw new Exception("Cannot call Save-/Load- Player APIs on other players!");
			GameManager.CurrentProfile.SetPlayerDataAsync(key.GetHashCode(), [((byte)(data ? 1 : 0))]).ConfigureAwait(false);
		}
		[Lua([Security.Capability.None])]
		public int LoadNumber(string key)
		{
			if (!IsLocalPlayer)
				throw new Exception("Cannot call Save-/Load- Player APIs on other players!");
			return BitConverter.ToInt32(GameManager.CurrentProfile.GetPlayerDataAsync(key.GetHashCode()).WaitAndGetResult());
		}
		[Lua([Security.Capability.None])]
		public string LoadString(string key)
		{
			if (!IsLocalPlayer)
				throw new Exception("Cannot call Save-/Load- Player APIs on other players!");
			return Encoding.UTF8.GetString(GameManager.CurrentProfile.GetPlayerDataAsync(key.GetHashCode()).WaitAndGetResult()!);
		}
		[Lua([Security.Capability.None])]
		public bool LoadBool(string key)
		{
			if (!IsLocalPlayer)
				throw new Exception("Cannot call Save-/Load- Player APIs on other players!");
			return GameManager.CurrentProfile.GetPlayerDataAsync(key.GetHashCode()).WaitAndGetResult()![0] == 1;
		}
		[Lua([Security.Capability.RobloxScriptSecurity])]
		public void SetUserId(long userid) => userId = userid;
		[Lua([Security.Capability.RobloxScriptSecurity])]
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

				if (GameManager.NetworkManager.IsServer)
					GameManager.NetworkManager.AddReplication(cl, Replication.REPM_TOALL, Replication.REPW_NEWINST);
			}

			var sp = Root.GetService<StarterPack>().GetChildren();
			for (int i = 0; i < sp.Length; i++)
			{
				var cl = sp[i].Clone();
				if (cl == null) return;
				cl.Parent = bc;

				if (GameManager.NetworkManager.IsServer)
					GameManager.NetworkManager.AddReplication(cl, Replication.REPM_TOALL, Replication.REPW_NEWINST);
			}

			LogManager.LogInfo("Reloaded " + Name + "'s backpack and GUI!");
		}
		[Lua([Security.Capability.CoreSecurity])]
		public void LoadCharacterOld()
		{
			if (!GameManager.NetworkManager.IsServer)
				throw new ScriptRuntimeException("Cannot call LoadCharacter from client!");

			var ch = new Character(GameManager);
			var face = new Decal(GameManager);
			var workspace = Root.GetService<Workspace>();

			if (Character != null)
				Character.Destroy();

			ch.Name = Name;
			ch.Color3 = GetPlayerColor().Color;
			ch.Position = workspace.SpawnLocation != null ? 
				workspace.SpawnLocation.Position
					+ new Vector3(0, workspace.SpawnLocation.Size.Y / 2 + 1, 0)
					+ new Vector3(0, 20, 0) : 
				new Vector3(0, 20, 0);
			ch.Parent = workspace;
			face.Texture = "rbxasset://textures/smile.png";
			face.Face = Faces.Front;
			face.Parent = ch;

			if (workspace.CurrentCamera != null)
				(workspace.CurrentCamera as Camera)!.CameraSubject = ch;

			Character = ch;
		}
		[Lua([Security.Capability.CoreSecurity])]
		public void LoadCharacter()
		{
			var workspace = Root.GetService<Workspace>();
			var chmodel = Root.GetService<PlatformService>().SpawnCharacterFor(CharacterAppearanceId, Name);

			if (workspace.SpawnLocation != null)
				chmodel.MoveTo(workspace.SpawnLocation.Position + new Vector3(0, 3.5f, 0));
			else
				chmodel.MoveTo(new Vector3(0, 10, 0));

			chmodel.Parent = workspace;

			if (GameManager.NetworkManager.IsServer)
				GameManager.NetworkManager.AddReplication(chmodel, Replication.REPM_TOALL, Replication.REPW_NEWINST);

			Character = chmodel;
		}
		[Lua([Security.Capability.None])]
		public BrickColor GetPlayerColor()
		{
			return Root.GetService<PlatformService>().GetPlayerColor(CharacterAppearanceId);
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

			if (!Client.IsAboutToLeave)
			{
				if (!WasKicked && (IsLocalPlayer || GameManager.NetworkManager.IsServer))
					Kick("Player has been removed from this DataModel");
			}
		}
	}
}
