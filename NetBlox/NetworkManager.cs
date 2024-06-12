using NetBlox.Common;
using NetBlox.Instances;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Structs;
using Network;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace NetBlox
{
	public sealed class NetworkManager
	{
		[StructLayout(LayoutKind.Sequential)]
		private unsafe struct ClientHandshake
		{
			public fixed char Username[24];
			public ushort VersionMajor;
			public ushort VersionMinor;
			public ushort VersionPatch;
		}
		private unsafe struct ServerHandshake
		{
			public fixed char PlaceName[36];
			public fixed char UniverseName[36];
			public fixed char Author[36];

			public ulong PlaceID;
			public ulong UniverseID;
			public uint MaxPlayerCount;
			public uint UniquePlayerID;

			public int ErrorCode;

			public int InstanceCount;
			public fixed byte DataModelInstance[16];
			public fixed byte PlayerInstance[16];
			public fixed byte CharacterInstance[16];
		}
		public class Replication
		{
			public int Mode;
			public int What;
			public NetworkClient[] Recievers = [];
			public Instance Target;
			public bool ReplicateChildren = true;

			public const int REPM_TOALL = 0;
			public const int REPM_BUTOWNER = 1;
			public const int REPM_TORECIEVERS = 2;

			public const int REPW_NEWINST = 0;
			public const int REPW_PROPCHG = 1;
			public const int REPW_REPARNT = 2;

			public Replication(int m, int w, Instance t)
			{
				Mode = m;
				What = w;
				Target = t;
			}
		}

		public GameManager GameManager;
		public List<NetworkClient> Clients = [];
		public Queue<Replication> ReplicationQueue = [];
		public bool IsServer;
		public bool IsClient;
		public int ServerPort = 2557; // new port lol
		public int ClientPort = 6553;
		public Connection? RemoteConnection;
		public ServerConnectionContainer Server;
		private static Type LST = typeof(LuaSignal);
		private DataModel Root => GameManager.CurrentRoot;
		private uint nextpid = 0;
		private bool init;

		public NetworkManager(GameManager gm, bool server, bool client)
		{
			GameManager = gm;
			if (!init)
			{
				IsServer = server;
				IsClient = client;
				init = true;
			}
		}
		/// <summary>
		/// Starts NetBlox server on current <seealso cref="NetworkManager"/>, this function is blocking, start it in other thread.
		/// </summary>
		public unsafe void StartServer()
		{
			if (!IsServer)
				throw new NotSupportedException("Cannot start server in non-server configuration!");

			Server = ConnectionFactory.CreateServerConnectionContainer(ServerPort);
			Server.AllowUDPConnections = false;
			Server.ConnectionEstablished += (x, y) =>
			{
				x.EnableLogging = false;
				LogManager.LogInfo(x.IPRemoteEndPoint.Address + " is trying to connect");
				bool gothandshake = false;

				x.RegisterRawDataHandler("nb2-handshake", (_x, _) =>
				{
					byte[] data = _x.Data;
					ClientHandshake ch;

					gothandshake = true;

					fixed (byte* d = &data[0])
						ch = Marshal.PtrToStructure<ClientHandshake>((nint)d);

					// as per to constitution, we must immediately disconnect the client if version mismatch happens

					if (ch.VersionMajor != Common.Version.VersionMajor ||
						ch.VersionMinor != Common.Version.VersionMinor ||
						ch.VersionPatch != Common.Version.VersionPatch)
					{
						LogManager.LogWarn(x.IPRemoteEndPoint.Address + " has wrong version! disconnecting...");
						x.Close(Network.Enums.CloseReason.DifferentVersion);
						return;
					}

					ServerHandshake sh = new();
					NetworkClient nc = new();
					nc.Connection = x;
					nc.UniquePlayerID = nextpid++;
					nc.Username = new string(ch.Username);
					nc.IsDisconnecting = false;

					// here we do a lot of shit
					{
						Player pl = new(GameManager);
						pl.IsLocalPlayer = false;
						pl.Name = nc.Username;
						pl.Parent = Root.GetService<Players>();
						pl.Client = nc;
						nc.Player = pl;

						pl.Reload();
						pl.LoadCharacter();
						Character character = (Character)pl.Character;

						Marshal.Copy(
							Encoding.Unicode.GetBytes(GameManager.CurrentIdentity.Author), 0, (nint)sh.Author,
							GameManager.CurrentIdentity.Author.Length * 2); // optimization? more like dick sucking
						Marshal.Copy(
							Encoding.Unicode.GetBytes(GameManager.CurrentIdentity.UniverseName), 0, (nint)sh.UniverseName,
							GameManager.CurrentIdentity.UniverseName.Length * 2);
						Marshal.Copy(
							Encoding.Unicode.GetBytes(GameManager.CurrentIdentity.PlaceName), 0, (nint)sh.PlaceName,
							GameManager.CurrentIdentity.PlaceName.Length * 2);
						Marshal.Copy(pl.UniqueID.ToByteArray(), 0, (nint)sh.PlayerInstance, 16);
						Marshal.Copy(character.UniqueID.ToByteArray(), 0, (nint)sh.CharacterInstance, 16);
						Marshal.Copy(Root.UniqueID.ToByteArray(), 0, (nint)sh.DataModelInstance, 16);

						sh.MaxPlayerCount = GameManager.CurrentIdentity.MaxPlayerCount;
						sh.UniverseID = GameManager.CurrentIdentity.UniverseID;
						sh.PlaceID = GameManager.CurrentIdentity.PlaceID;
						sh.UniquePlayerID = nc.UniquePlayerID;

						Clients.Add(nc);
					}

					x.SendRawData("nb2-placeinfo", ValueTypeExtensions.GetBytes(sh));
					x.UnRegisterRawDataHandler("nb2-handshake");

					bool acked = false;

					x.RegisterRawDataHandler("nb2-init", (_, _) =>
					{
						acked = true;

						AddReplication(Root, Replication.REPM_TORECIEVERS, Replication.REPW_NEWINST, false, [nc]);
						AddReplication(Root.GetService<ReplicatedFirst>(), Replication.REPM_TORECIEVERS, Replication.REPW_NEWINST, true, [nc]);
						AddReplication(Root.GetService<Workspace>(), Replication.REPM_TORECIEVERS, Replication.REPW_NEWINST, true, [nc]);
						AddReplication(Root.GetService<ReplicatedStorage>(), Replication.REPM_TORECIEVERS, Replication.REPW_NEWINST, true, [nc]);
						AddReplication(Root.GetService<Lighting>(), Replication.REPM_TORECIEVERS, Replication.REPW_NEWINST, true, [nc]);
						AddReplication(Root.GetService<Players>(), Replication.REPM_TORECIEVERS, Replication.REPW_NEWINST, true, [nc]);
						AddReplication(Root.GetService<StarterGui>(), Replication.REPM_TORECIEVERS, Replication.REPW_NEWINST, true, [nc]);
						AddReplication(Root.GetService<StarterPack>(), Replication.REPM_TORECIEVERS, Replication.REPW_NEWINST, true, [nc]);

						x.UnRegisterRawDataHandler("nb2-init"); // as per to constitution, we now do nothing lol
					});

					Task.Delay(5000).ContinueWith(_ =>
					{
						if (!acked)
						{
							LogManager.LogWarn(x.IPRemoteEndPoint.Address + " didn't acknowledge server handshake! disconnecting...");
							x.Close(Network.Enums.CloseReason.NetworkError);
							return;
						}
					});
				});

				Task.Delay(5000).ContinueWith(_ =>
				{
					if (!gothandshake)
					{
						LogManager.LogWarn(x.IPRemoteEndPoint.Address + " didn't send handshake! disconnecting...");
						x.Close(Network.Enums.CloseReason.NetworkError);
						return;
					}
				});
			};

			Server.Start();

			// but actually we are not done

			while (!GameManager.ShuttingDown)
			{
				while (ReplicationQueue.Count == 0) ;
				lock (ReplicationQueue)
				{
					var rq = ReplicationQueue.Dequeue();
					var rc = rq.Recievers;
					var ins = rq.Target;

					switch (rq.Mode)
					{
						case Replication.REPM_TOALL:
							rc = Clients.ToArray();
							break;
						case Replication.REPM_BUTOWNER:
							rc = (NetworkClient[])Clients.ToArray().Clone();
							if (ins.NetworkOwner != null)
								rc.ToList().Remove(ins.NetworkOwner);
							break;
						case Replication.REPM_TORECIEVERS:
							break;
					}

					switch (rq.What)
					{
						case Replication.REPW_NEWINST:
							PerformReplicationNew(ins, rc);
							break;
						case Replication.REPW_PROPCHG:
							PerformReplicationPropchg(ins, rc); // i found constitutional loophole, im not required to send only changed props, i can send entire instance bc idc
							break;
						case Replication.REPW_REPARNT:
							PerformReplicationReparent(ins, rc);
							break;
					}
				}
			}
		}
		public void ConnectToServer(IPAddress ipa)
		{
			if (IsServer)
				throw new NotSupportedException("Cannot teleport in server");



		}
		private void PerformReplicationNew(Instance ins, NetworkClient[] recs)
		{
			using MemoryStream ms = new();
			using BinaryWriter bw = new(ms);
			var gm = ins.GameManager;
			var type = ins.GetType(); // apparently gettype caches type object but i dont believe
			var props = type.GetProperties();

			bw.Write(SerializationManager.NetworkSerialize(ins.UniqueID, gm));
			bw.Write(SerializationManager.NetworkSerialize(ins.ParentID, gm));

			var c = 0;

			for (int i = 0; i < props.Length; i++)
			{
				var prop = props[i];
				if (prop.PropertyType == LST)
					continue;

				if (!SerializationManager.NetworkSerializers.TryGetValue(prop.PropertyType.FullName, out var x))
					continue;

				c++;
				var b = x(prop.GetValue(ins), gm);

				bw.Write((short)prop.Name.Length);
				bw.Write(SerializationManager.NetworkSerialize(prop.Name, gm));
				bw.Write((short)b.Length);
				bw.Write(b);
			}

			bw.Write((byte)c);

			byte[] buf = ms.ToArray();

			for (int i = 0; i < recs.Length; i++)
			{
				var nc = recs[i];
				var con = nc.Connection;
				if (con == null) continue; // how did this happen

				con.SendRawData("nb2-replicate", buf);
			}
		}
		private void PerformReplicationPropchg(Instance ins, NetworkClient[] recs)
		{
			PerformReplicationNew(ins, recs); // same thing really
		}
		private unsafe struct ReparentPacket
		{
			public fixed byte Target[16];
			public fixed byte Parent[16];
		}
		private unsafe void PerformReplicationReparent(Instance ins, NetworkClient[] recs)
		{
			ReparentPacket rp = new();
			Marshal.Copy(ins.UniqueID.ToByteArray(), 0, (nint)rp.Target, 16);
			Marshal.Copy(ins.ParentID.ToByteArray(), 0, (nint)rp.Parent, 16);
			var bytes = ValueTypeExtensions.GetBytes(rp);

			for (int i = 0; i < recs.Length; i++)
			{
				var nc = recs[i];
				var con = nc.Connection;
				if (con == null) continue; // how did this happen

				con.SendRawData("nb2-reparent", bytes);
			}
		}
		public void AddReplication(Instance inst, int m, int w, bool rc = true, NetworkClient[]? nc = null)
		{
			ReplicationQueue.Enqueue(new(m, w, inst)
			{
				Recievers = nc ?? [],
				ReplicateChildren = rc
			});
		}
	}
}
