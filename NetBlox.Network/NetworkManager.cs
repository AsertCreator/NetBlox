using NetBlox.Instances;
using NetBlox.Instances.Services;
using NetBlox.Network;
using NetBlox.Runtime;
using NetBlox.Structs;
using Network;
using Network.Enums;
using Network.Extensions;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetBlox.Network
{
	public struct ClientHandshake
	{
		public string? Username;
		public int VersionMajor;
		public int VersionMinor;
		public int VersionPatch;
	}
	public struct ServerHandshake
	{
		public string PlaceName;
		public string UniverseName;
		public string Author;

		public ulong PlaceID;
		public ulong UniverseID;
		public uint MaxPlayerCount;
		public uint UniquePlayerID;

		public int ErrorCode;
		public string ErrorMessage;

		public int InstanceCount;
		public Guid DataModelInstance;
		public Guid PlayerInstance;
		public Guid CharacterInstance;
	}
	public class Replication
	{
		public List<RemoteClient> To;
		public Instance? What;
		public bool RepChildren = true;
		public bool AsService = false;
		public Action? Callback;
	}
	public class NetworkManager
	{
		private static readonly JsonSerializerOptions DefaultJSON = new()
		{
			IncludeFields = true
		};
		public TcpClient RemoteClient = null!;
		public TcpListener NetworkServer = null!;
		public GameManager GameManager;
		public bool IsServer { get; private set; }
		public bool IsClient { get; private set; }
		public int ServerPort { get; private set; } = 2556;
		public int ClientPort { get; private set; } = 6552;
		public Queue<Replication> ToReplicate = new();
		public Connection? ServerConnection;
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

		public void ResetClient(IPAddress ip)
		{
			NetworkClient nc = new(this);
			nc.Start(ip, ServerPort);
		}
		public byte[] SerializeJsonBytes<T>(T obj) => Encoding.UTF8.GetBytes(SerializeJson(obj));
		public string SerializeJson<T>(T obj) => JsonSerializer.Serialize(obj, DefaultJSON);
		public T? DeserializeJsonBytes<T>(byte[] d) => DeserializeJson<T>(Encoding.UTF8.GetString(d));
		public T? DeserializeJson<T>(string d) => JsonSerializer.Deserialize<T>(d, DefaultJSON);
		public void SeqReparentInstance(Connection c, Instance ins)
		{
			try
			{
				var dss = new Dictionary<string, string>();
				dss["Instance"] = ins.UniqueID.ToString();
				dss["Parent"] = ins.ParentID.ToString();
				c.SendRawData("nb.repar-inst", SerializeJsonBytes(dss));
			}
			catch
			{
				LogManager.LogError($"Failed to perform network instance reparent ({ins.UniqueID}->{ins.ParentID})!");
			}
		}
		public void SeqReplicateInstance(Connection c, Instance ins, bool repchildren, bool asservice)
		{
			try
			{
				var dss = new Dictionary<string, string>();
				var typ = ins.GetType();
				var prs = typ.GetProperties();

				if (typ.GetCustomAttribute<NotReplicatedAttribute>() != null) // we dont do THAT
					return;

				for (int i = 0; i < prs.Length; i++)
				{
					var prop = prs[i];
					var val = prop.GetValue(ins);
					if (prop.GetCustomAttribute<NotReplicatedAttribute>() != null) continue;
					if (val == null) continue;

					dss[prop.Name] = SerializationManager.Serialize(val);
				}

				var key = "nb." + ins.UniqueID;

				c.RegisterRawDataHandler(key, (x, y) =>
				{
					y.UnRegisterRawDataHandler(key);
					if (repchildren)
					{
						var ch = ins.GetChildren();
						for (int i = 0; i < ch.Length; i++)
							SeqReplicateInstance(c, ch[i], true, false);
					}
				});

				if (!asservice)
					c.SendRawData("nb.inc-inst", SerializeJsonBytes(dss));
				else
					c.SendRawData("nb.inc-service", SerializeJsonBytes(dss));
			}
			catch
			{
				LogManager.LogError($"Failed to replicate instance ({ins.UniqueID})!");
			}
		}
		public Instance SeqReceiveInstance(Connection c, string tag)
		{
			var inf = JsonSerializer.Deserialize<Dictionary<string, string>>(tag);
			var uid = Guid.Parse(inf["UniqueID"]);
			var pre = (from x in GameManager.AllInstances where x.UniqueID == uid select x).FirstOrDefault();
			if (pre == null)
			{
				var ins = InstanceCreator.CreateInstance(inf["ClassName"], GameManager);
				var typ = ins.GetType();
				var prs = typ.GetProperties();

				ins.WasReplicated = true;

				for (int i = 0; i < inf.Count; i++)
				{
					try
					{
						var kvp = inf.ElementAt(i);
						var prop = Array.Find(prs, x => x.Name == kvp.Key);
						if (prop == null) continue;
						if (prop.CanWrite)
							prop.SetValue(ins, SerializationManager.Deserialize(prop.PropertyType, inf[kvp.Key]));
					}
					catch
					{
						// we had failed
					}
				}

				ins.Parent = (from x in GameManager.AllInstances where x.UniqueID == ins.ParentID select x).FirstOrDefault();

				var key = "nb." + ins.UniqueID;

				c.SendRawData(key, []);

				return ins;
			}
			else
			{
				var typ = pre.GetType();
				var prs = typ.GetProperties();

				for (int i = 0; i < inf.Count; i++)
				{
					try
					{
						var kvp = inf.ElementAt(i);
						var prop = Array.Find(prs, x => x.Name == kvp.Key);
						if (prop == null) continue;
						if (prop.CanWrite)
							prop.SetValue(pre, SerializationManager.Deserialize(prop.PropertyType, inf[kvp.Key]));
					}
					catch
					{
						// we had failed
					}
				}

				return pre;
			}
		}
	}
}
