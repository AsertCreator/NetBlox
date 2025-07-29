using NetBlox.Instances;
using NetBlox.Instances.Scripts;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using System.Reflection;

namespace NetBlox.Network
{
	public class Replication
	{
		public int Mode;
		public int What;
		public RemoteClient[] Recievers = [];
		public PropertyInfo[] Properties = [];
		public Instance Target;

		public const int REPM_TOALL = 0;
		public const int REPM_BUTOWNER = 1;
		public const int REPM_TORECIEVERS = 2;

		public const int REPW_NEWINST = 0;
		public const int REPW_PROPCHG = 1;
		public const int REPW_REPARNT = 2;
		public const int REPW_DESTROY = 3;

		public static Dictionary<(RemoteClient, Guid), Action> AwaitingInstanceMap = [];

		public Replication(int m, int w, Instance t)
		{
			Mode = m;
			What = w;
			Target = t;
		}

		public static void ApplyFromBytes(GameManager gm, RemoteClient? sender, int mode, int what, byte[] bytes)
		{
			void ApplyFromBytesImpl()
			{
				if (what == REPW_DESTROY)
					ApplyDestroy(gm, sender, new Guid(bytes));
				else if (what == REPW_REPARNT)
					ApplyReparent(gm, sender, new Guid(bytes[0..16]), new Guid(bytes[16..32]));
				else if (what == REPW_NEWINST || what == REPW_PROPCHG)
					ApplyChanges(gm, sender, bytes);
			}
			if (gm.NetworkManager.SynchronousReplication)
			{
				TaskScheduler.Schedule(ApplyFromBytesImpl);
			}
			else
			{
				lock (LuaRuntime.GlobalLock)
					ApplyFromBytesImpl();
			}
		}
		private static void ApplyChanges(GameManager gm, RemoteClient? sender, byte[] data)
		{
			using MemoryStream ms = new(data);
			using BinaryReader br = new(ms);

			int propc = data[^1];
			Guid guid = new(br.ReadBytes(16));
			Guid newp = new(br.ReadBytes(16));
			var ins = gm.GetInstance(guid);
			var classname = br.ReadString();

			if (ins == null)
			{
				ins = InstanceCreator.CreateReplicatedInstance(classname, gm);
				if (ins is BaseScript && gm.NetworkManager.IsServer)
				{
					LogManager.LogWarn("A client (" + sender + ") tried to replicate a script to server!");
					return;
				}
				ins.Parent = gm.GetInstance(newp);
				if (guid == gm.NetworkManager.ExpectedLocalPlayerGuid)
				{
					var player = ins as Player;
					var players = gm.CurrentRoot.GetService<Players>();
					players.CurrentPlayer = player;
					player.IsLocalPlayer = true;
				}
			}

			ins.UniqueID = guid;
			ins.WasReplicated = true;

			var type = ins.GetType();
			var impattrib = type.GetCustomAttribute<ImpersonateDuringReplicationAttribute>();

			if (impattrib != null)
				Security.Impersonate(impattrib.Level);

			for (int i = 0; i < propc; i++)
			{
				string propname = br.ReadString();
				var prop = type.GetProperty(propname);
				if (prop == null) // how did this happen
					continue;

				var ptyp = prop.PropertyType;
				var pnam = ptyp.FullName ?? "";
				var bc = br.ReadInt16();
				var pbytes = br.ReadBytes(bc);

				if (!prop.CanWrite)
					continue;

				if (ptyp.IsAssignableTo(NetworkManager.InstanceType))
				{
					var propguid = new Guid(pbytes);
					var inst = gm.GetInstance(propguid);

					if (inst == null)
					{
						gm.NetworkManager.AwaitingForArrival[propguid] = () =>
						{
							gm.NetworkManager.AwaitingForArrival[propguid] = null;
							inst = gm.GetInstance(propguid);
							prop.SetValue(ins, inst);
						};
					}
					else
						prop.SetValue(ins, inst);
				}
				else
				{
					if (SerializationManager.NetworkDeserializers.TryGetValue(pnam, out var x))
						prop.SetValue(ins, x(pbytes, gm));
				}
			}

			if (impattrib != null)
				Security.EndImpersonate();

			if (gm.NetworkManager.LoadedInstanceCount++ >= gm.NetworkManager.TargetInstanceCount && !gm.NetworkManager.IsLoaded)
			{
				gm.NetworkManager.IsLoaded = true;
				gm.CurrentRoot.GetService<CoreGui>().HideTeleportGui();
			}
			if (gm.NetworkManager.AwaitingForArrival.TryGetValue(guid, out var act))
				act();
			if (ins is Workspace ws)
			{
				Camera cam = new Camera(ws.GameManager);
				cam.Parent = ws;
				ws.CurrentCamera = cam;
			}
		}
		private static void ApplyReparent(GameManager gm, RemoteClient? sender, Guid unique, Guid parent)
		{
			Instance instance = gm.GetInstance(unique);
			Instance newparent = gm.GetInstance(parent);

			if (gm.NetworkManager.IsServer) // actually fuck off
			{
				LogManager.LogWarn("A client (" + sender + ") tried to send a reparent packet to server!");
				return;
			}

			instance.Parent = newparent;
		}
		private static void ApplyDestroy(GameManager gm, RemoteClient? sender, Guid unique)
		{
			Instance instance = gm.GetInstance(unique);

			if (gm.NetworkManager.IsServer)
			{
				LogManager.LogWarn("A client (" + sender + ") tried to send a destruction packet to server!");
				return;
			}

			instance.Destroy();
		}

		public byte[] Serialize()
		{
			if (What == REPW_NEWINST)
			{
				Properties = null;
				return SerializeChanged(null);
			}
			else if (What == REPW_PROPCHG)
			{
				Properties = null;
				return SerializeChanged(Properties);
			}
			else if (What == REPW_DESTROY)
				return SerializeDestroy();
			else if (What == REPW_REPARNT)
				return SerializeReparent();
			throw new NotSupportedException("what");
		}
		private byte[] SerializeReparent()
		{
			List<byte> bytes = [];
			bytes.AddRange(Target.UniqueID.ToByteArray());
			bytes.AddRange(Target.ParentID.ToByteArray());
			return bytes.ToArray();
		}
		private byte[] SerializeDestroy() => Target.UniqueID.ToByteArray();
		private byte[] SerializeChanged(PropertyInfo[]? props)
		{
			using MemoryStream ms = new();
			using BinaryWriter bw = new(ms);
			var gm = Target.GameManager;
			var type = Target.GetType(); // apparently gettype caches type object but i dont believe

			bw.Write(Target.UniqueID.ToByteArray());
			bw.Write(Target.ParentID.ToByteArray());
			bw.Write(Target.ClassName);

			var c = 0;

			if (props is null)
				props = type.GetProperties();

			for (int i = 0; i < props.Length; i++)
			{
				var prop = props[i];

				if (prop.GetCustomAttribute<NotReplicatedAttribute>() != null)
					continue;
				if (!prop.CanWrite)
					continue;
				if (prop.PropertyType.Name == "LuaSignal")
					continue;

				byte[] b;

				if (prop.PropertyType.IsAssignableTo(NetworkManager.InstanceType))
				{
					if (!SerializationManager.NetworkSerializers.TryGetValue("NetBlox.Instances.Instance", out var x))
						continue;

					var v = prop.GetValue(Target);
					if (v == null) continue;
					c++;
					b = x(v, gm);
				}
				else
				{
					if (!SerializationManager.NetworkSerializers.TryGetValue(prop.PropertyType.FullName ?? "", out var x))
						continue;

					var v = prop.GetValue(Target);
					if (v == null) continue;
					c++;
					b = x(v, gm);
				}

				bw.Write(prop.Name);
				bw.Write((short)b.Length);
				bw.Write(b);
			}

			bw.Write((byte)c);

			return ms.ToArray();
		}
	}
}
