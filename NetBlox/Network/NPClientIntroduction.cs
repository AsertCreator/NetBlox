using NetBlox.Instances;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using Network.Enums;
using System.Text;

namespace NetBlox.Network
{
	public class NPClientIntroduction : NetworkPacketHandler
	{
		public override int ProbeTargetPacketId => TargetPacketId;

		public const int TargetPacketId = (int)NetworkPacketTypeEnum.NPClientIntroduction;

		private struct ClientHandshake
		{
			public string Username;
			public string Authorization;
			public ushort VersionMajor;
			public ushort VersionMinor;
			public ushort VersionPatch;
		}

		public static NetworkPacket Create(string username, Dictionary<string, string> authorization)
		{
			using MemoryStream stream = new();
			using BinaryWriter writer = new(stream);

			var authstring = SerializationManager.SerializeJson(authorization);

			writer.Write(username);
			writer.Write(authstring);
			writer.Write((ushort)Common.Version.VersionMajor);
			writer.Write((ushort)Common.Version.VersionMinor);
			writer.Write((ushort)Common.Version.VersionPatch);

			return new NetworkPacket(TargetPacketId, stream.ToArray(), null);
		}

		public override void HandleClientbound(GameManager gm, NetworkPacket packet, BinaryReader reader) { }
		public override void HandleServerbound(GameManager gm, NetworkPacket packet, BinaryReader reader)
		{
			ClientHandshake handshake = new();
			handshake.Username = reader.ReadString();
			handshake.Authorization = reader.ReadString();
			handshake.VersionMajor = reader.ReadUInt16();
			handshake.VersionMinor = reader.ReadUInt16();
			handshake.VersionPatch = reader.ReadUInt16();

			// nobody will pass

			if (handshake.VersionMajor != Common.Version.VersionMajor ||
				handshake.VersionMinor != Common.Version.VersionMinor ||
				handshake.VersionPatch != Common.Version.VersionPatch)
			{
				LogManager.LogWarn(packet.Sender + " has wrong version! disconnecting...");
				packet.Sender.KickOut("Your NetBlox client version is incompatible with this server!");
				return;
			}

			var rc = packet.Sender;
			var errcode = 0;
			var stoppls = false;
			var isguest = false;
			var userid = -1L;
			var address = rc.Connection.IPRemoteEndPoint.Address.ToString();
			var authdata = SerializationManager.DeserializeJson<Dictionary<string, string>>(handshake.Authorization);

			if (authdata == null)
			{
				LogManager.LogWarn(rc + " didn't pass authorization data! disconnecting...");
				errcode = 102;
				stoppls = true;
				return;
			}
			if (authdata.TryGetValue("isguest", out string val) && bool.Parse(val)) isguest = true;
			if (authdata.TryGetValue("userid", out string uid)) userid = long.Parse(uid);

			if (!(isguest ^ (userid != -1)))
			{
				LogManager.LogWarn(rc + "'s authorization data contains both guest and account data! disconnecting...");
				errcode = 102;
				stoppls = true;
				return;
			}

			if (gm.NetworkManager.OnlyInternalConnections && address != "::ffff:127.0.0.1")
			{
				errcode = 101;
				stoppls = true;
			}
			if (gm.NetworkManager.Clients.Count < gm.CurrentIdentity.MaxPlayerCount && !stoppls)
			{
				Security.Impersonate(8);
				var id = isguest ? Random.Shared.Next(-100000, -1) : userid;
				var plrs = gm.CurrentRoot.GetService<Players>();
				var allplrs = plrs.GetChildren();

				for (int i = 0; i < allplrs.Length; i++)
				{
					if (allplrs[i].Name.Trim() == handshake.Username.Trim())
					{
						errcode = 103;
						stoppls = true;
						break;
					}
				}

				if (!stoppls)
				{
					rc.Username = handshake.Username;

					var player = new Player(rc.Enclosure)
					{
						IsLocalPlayer = false,
						Parent = plrs,
						CharacterAppearanceId = id,
						Name = rc.Username,
						Client = rc
					};

					player.SetUserId(id);
					rc.Player = player;

					Security.EndImpersonate();

					player.Reload();
					player.LoadCharacter();

					var root = gm.CurrentRoot;
					var netmgr = gm.NetworkManager;
					var toReceivers = Replication.REPM_TORECIEVERS;
					var newInstance = Replication.REPW_NEWINST; // i will not tolerate code that doesn't fit on my 15 inch fhd screen

					netmgr.AddReplication(root.GetService<ReplicatedFirst>(), toReceivers, newInstance, true, [rc]);
					netmgr.AddReplication(root.GetService<ReplicatedStorage>(), toReceivers, newInstance, true, [rc]);
					netmgr.AddReplication(root.GetService<Chat>(), toReceivers, newInstance, true, [rc]);
					netmgr.AddReplication(root.GetService<Lighting>(), toReceivers, newInstance, true, [rc]);
					netmgr.AddReplication(root.GetService<Players>(), toReceivers, newInstance, true, [rc]);
					netmgr.AddReplication(root.GetService<StarterGui>(), toReceivers, newInstance, true, [rc]);
					netmgr.AddReplication(root.GetService<StarterPack>(), toReceivers, newInstance, true, [rc]);
					netmgr.AddReplication(root.GetService<Workspace>(), toReceivers, newInstance, true, [rc]);
				}
			}
			else if (!stoppls)
			{
				errcode = 100;
			}

			var shpacket = NPServerIntroduction.Create(gm, rc, errcode);
			rc.SendPacket(shpacket);
		}
	}
}
