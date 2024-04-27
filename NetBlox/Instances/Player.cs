using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Instances;
using System.Text.Json.Serialization;

namespace NetBlox.Instances
{
	[Creatable]
	public class Player : Instance
	{
		[Lua]
		[JsonIgnore]
		public Instance? Character { get; set; }
		[JsonIgnore]
		public bool IsLocalPlayer;

		[Lua]
		public void LoadCharacter()
		{
			var ch = new Character();
			var workspace = GameManager.CurrentRoot!.FindFirstChild("Workspace");

			if (Character != null)
				Character.Destroy();

			if (workspace == null)
			{
				LogManager.LogError("No Workspace exists for Character!");
				return;
			}

			var actws = (Workspace)workspace;

			ch.Parent = workspace;
			ch.Name = Name;

			if (actws.MainCamera != null)
				(actws.MainCamera as Camera)!.CameraSubject = ch;

			Character = ch;
		}
		[Lua]
		public void Kick(string msg)
		{
			if (IsLocalPlayer)
			{
				NetworkManager.ServerConnection.Close(Network.Enums.CloseReason.ClientClosed);
				RenderManager.ShowKickMessage(msg);
				// why not call lua api lol
				GameManager.GetService<RunService>().Pause();
			}
			else if (NetworkManager.IsServer)
			{
				// do smth
			}
			else
			{
				// we're trying to kick another player from client
				throw new Exception("Cannot kick non-local player from client");
			}
		}
		[Lua]
		public override bool IsA(string classname)
		{
			if (nameof(Player) == classname) return true;
			return base.IsA(classname);
		}
		[Lua]
		public override void Destroy() // also destroy character
		{
			base.Destroy();
			Character.Destroy();
			Kick("Player has been removed from this DataModel");
		}
	}
}
