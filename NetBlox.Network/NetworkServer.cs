using NetBlox.Instances;
using NetBlox.Instances.Services;
using Network;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NetBlox.Network
{
	public class NetworkServer
	{
		public NetworkManager NetworkManager;
		public List<RemoteClient> Clients = [];
		private uint NextPID = 0;

		public NetworkServer(NetworkManager nm) 
		{
			NetworkManager = nm;
		}
		public void Disconnect(RemoteClient nc)
		{
			nc.IsDisconnecting = true;
			if (nc.Player != null)
			{
				nc.Player.Character?.Destroy();
				nc.Player.Destroy();
			}
			LogManager.LogInfo($"{nc.Username} had disconnected!");
			Clients.Remove(nc);
		}
		public void Start(int port)
		{
			if (!NetworkManager.IsServer)
				throw new NotSupportedException("Cannot start server in non-server configuration!");

			LogManager.LogInfo($"Starting listening for server connections at {port}...");
			ServerConnectionContainer scc = ConnectionFactory.CreateServerConnectionContainer(port);
			scc.ConnectionEstablished += (_x, _y) =>
			{
				LogManager.LogInfo($"Connection established with {_x.IPRemoteEndPoint.Address}!");
				_x.RegisterRawDataHandler("nb.handshake", (x, y) =>
				{
					ServerHandshake sh = new ServerHandshake();
					ClientHandshake ch = NetworkManager.DeserializeJsonBytes<ClientHandshake>(x.Data);
					RemoteClient nc = new RemoteClient();
					Players pls = NetworkManager.GameManager.CurrentRoot.GetService<Players>()!;
					Backpack bck = new Backpack(NetworkManager.GameManager);
					PlayerGui pg = new PlayerGui(NetworkManager.GameManager);
					Player plr = new Player(NetworkManager.GameManager);

					nc.Username = ch.Username;
					nc.Connection = y;
					nc.UniquePlayerID = NextPID++;
					nc.IsDisconnecting = false;
					nc.Player = plr;

					pg.Reload();
					bck.Reload();

					bck.Parent = plr;
					pg.Parent = plr;

					Clients.Add(nc);

					plr.Name = nc.Username ?? string.Empty;
					plr.Parent = pls;
					plr.LoadCharacter();

					sh.PlaceName = NetworkManager.GameManager.CurrentIdentity.PlaceName;
					sh.UniverseName = NetworkManager.GameManager.CurrentIdentity.UniverseName;
					sh.Author = NetworkManager.GameManager.CurrentIdentity.Author;
					sh.PlaceID = NetworkManager.GameManager.CurrentIdentity.PlaceID;
					sh.UniverseID = NetworkManager.GameManager.CurrentIdentity.UniverseID;
					sh.UniquePlayerID = nc.UniquePlayerID;
					sh.PlayerInstance = plr.UniqueID;
					sh.CharacterInstance = plr.Character.UniqueID;
					sh.DataModelInstance = NetworkManager.GameManager.CurrentRoot.UniqueID;
					sh.InstanceCount =
						NetworkManager.GameManager.CurrentRoot.GetService<ReplicatedFirst>().CountDescendants() +
						NetworkManager.GameManager.CurrentRoot.GetService<Players>().CountDescendants() +
						NetworkManager.GameManager.CurrentRoot.GetService<Workspace>().CountDescendants() +
						NetworkManager.GameManager.CurrentRoot.GetService<ReplicatedStorage>().CountDescendants();

					y.SendRawData("nb.placeinfo", NetworkManager.SerializeJsonBytes(sh));

					LogManager.LogInfo($"Successfully performed handshake with {ch.Username}!");

					Thread.Sleep(40);

					_x.ConnectionClosed += (f, g) =>
					{
						Disconnect(nc);
					};
					_x.RegisterRawDataHandler("nb.inc-inst", (x, y) =>
					{
						var ins = SeqReceiveInstance(_x, x.ToUTF8String());
						var to = GameManager.AllClients;
						to.Remove(nc);
						if (to.Count != 0)
						{
							lock (ToReplicate)
							{
								ToReplicate.Enqueue(new Replication()
								{
									To = to,
									What = ins
								});
							}
						}
					});
					_x.RegisterRawDataHandler("nb.req-int-rep", (x, y) =>
					{
						lock (ToReplicate)
						{
							ToReplicate.Enqueue(new Replication()
							{
								To = [nc],
								What = GameManager.CurrentRoot,
								RepChildren = false
							});
							ToReplicate.Enqueue(new Replication()
							{
								To = [nc],
								What = GameManager.CurrentRoot.GetService<ReplicatedFirst>(),
								AsService = true
							});
							ToReplicate.Enqueue(new Replication()
							{
								To = [nc],
								What = GameManager.CurrentRoot.GetService<Players>(),
								AsService = true
							});
							ToReplicate.Enqueue(new Replication()
							{
								To = [nc],
								What = GameManager.CurrentRoot.GetService<Workspace>(),
								AsService = true
							});
							ToReplicate.Enqueue(new Replication()
							{
								To = [nc],
								What = GameManager.CurrentRoot.GetService<ReplicatedStorage>(),
								AsService = true
							});
						}
					});

					GameManager.AllowReplication = true;
				});
			};
			Task.Run(() =>
			{
				while (!GameManager.ShuttingDown)
				{
					try
					{
						while (ToReplicate.Count == 0) ;
						lock (ToReplicate)
						{
							var tr = ToReplicate.Dequeue();
							var ins = tr.What;
							var to = tr.To ?? GameManager.AllClients;

							for (int i = 0; i < to.Count; i++)
								SeqReplicateInstance(to[i].Connection!, ins!, tr.RepChildren, tr.AsService);

							if (tr.Callback != null)
								tr.Callback();
						}
					}
					catch (Exception ex)
					{
						LogManager.LogError($"Could not replicate queued instance, well i dont care!");
					}
				}
			});
		}
	}
}
