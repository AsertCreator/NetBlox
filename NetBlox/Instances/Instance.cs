using MoonSharp.Interpreter;
using NetBlox.Instances.Services;
using NetBlox.Network;
using NetBlox.Runtime;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text;

namespace NetBlox.Instances
{
	public partial class Instance
	{
		[Lua([Security.Capability.None])]
		public virtual bool Archivable { get; set; } = true;
		[Lua([Security.Capability.None])]
		public virtual string ClassName => GetType().Name;
		[Lua([Security.Capability.None])]
		public virtual string Name { get; set; }
		[Lua([Security.Capability.None])]
		[NotReplicated]
		public virtual Instance? Parent
		{
			get => parent;
			set
			{
				lock (this)
				{
					if (WasDestroyed) return;

					if (parent != null)
					{
						lock (parent)
						{
							lock (parent.Children)
								parent.Children.Remove(this);
							if (GameManager.MainEnvironment != null)
							{
								parent.NativeChildRemoved?.Invoke(parent, this);
								parent.NativeDescendantRemoved?.Invoke(parent, this);
							}
						}
					}
					if (value != null)
					{
						lock (value)
						{
							parent = value;
							ParentID = parent.UniqueID;
							lock (value.Children)
								value.Children.Add(this);
							if (GameManager.MainEnvironment != null)
							{
								parent.NativeChildAdded?.Invoke(parent, this);
								parent.NativeDescendantAdded?.Invoke(parent, this);
							}
						}
					}
					else
					{
						parent = null;
						ParentID = Guid.Empty;
					}
				}
			}
		}
		[NotReplicated]
		public List<string> Tags { get; set; } = [];
		[NotReplicated]
		public Guid ParentID { get; set; }
		[NotReplicated]
		public Guid UniqueID { get; set; }
		[Lua([Security.Capability.None])]
		[NotReplicated]
		public LuaSignal DescendantAdded { get; init; }
		[Lua([Security.Capability.None])]
		[NotReplicated]
		public LuaSignal DescendantRemoved { get; init; }
		[Lua([Security.Capability.None])]
		[NotReplicated]
		public LuaSignal ChildAdded { get; init; }
		[Lua([Security.Capability.None])]
		[NotReplicated]
		public LuaSignal ChildRemoved { get; init; }
		[Lua([Security.Capability.None])]
		[NotReplicated]
		public LuaSignal Changed { get; init; }
		[Lua([Security.Capability.None])]
		[NotReplicated]
		public LuaSignal Destroying { get; init; }
		public bool EligibleForReplication 
		{ 
			get
			{
				if (Parent == null)
					return false;
				if (Parent is Workspace || Parent is ReplicatedFirst || Parent is ReplicatedStorage || Parent is Players ||
					Parent is Lighting || Parent is StarterGui || Parent is StarterPack)
					return true;
				return Parent.EligibleForReplication;
			} 
		}
		public virtual Security.Capability[] RequiredCapabilities => [];
		public bool WasDestroyed = false;
		public bool WasReplicated = false;
		public GameManager GameManager;
		public List<Instance> Children = [];
		public DateTime DestroyAt = DateTime.MaxValue;
		public DateTime DoNotReplicateUntil = DateTime.MinValue;
		public Dictionary<string, LuaSignal> ChangedSignals = [];
		public static Dictionary<int, Table> MetaTables = [];
		public Table? Table;

		public event EventHandler<Instance> NativeChildAdded;
		public event EventHandler<Instance> NativeChildRemoved;
		public event EventHandler<Instance> NativeDescendantAdded;
		public event EventHandler<Instance> NativeDescendantRemoved;

		private Instance? parent;
		private Type? ThisType;
		private bool containsInstanceReferences;
		protected DataModel Root => GameManager.CurrentRoot;

		public Instance(GameManager gm)
		{
			lock (this)
			{
				Name = ClassName;
				UniqueID = Guid.NewGuid();
				GameManager = gm;

				if (InstanceCreator.InstancesWithInstanceReferences.Contains(GetType()))
					containsInstanceReferences = true;

				gm.AllInstances.Add(this);
			}
			ThisType = GetType();

			DescendantAdded = new LuaSignal(gm);
			DescendantRemoved = new LuaSignal(gm);
			ChildAdded = new LuaSignal(gm);
			ChildRemoved = new LuaSignal(gm);
			Changed = new LuaSignal(gm);
			Destroying = new LuaSignal(gm);

			NativeChildAdded += (_, descendant) => ChildAdded.Fire(LuaRuntime.PushInstance(descendant));
			NativeChildRemoved += (_, descendant) => ChildRemoved.Fire(LuaRuntime.PushInstance(descendant));
			NativeDescendantAdded += (_, descendant) => DescendantAdded.Fire(LuaRuntime.PushInstance(descendant));
			NativeDescendantRemoved += (_, descendant) => DescendantRemoved.Fire(LuaRuntime.PushInstance(descendant));
			NativeDescendantAdded += (_, descendant) => Parent?.NativeDescendantAdded?.Invoke(_, descendant);
			NativeDescendantRemoved += (_, descendant) => Parent?.NativeDescendantRemoved?.Invoke(_, descendant);
		}
		public virtual void Process()
		{
			// process nothing
		}
		public virtual void RenderUI()
		{
			// render nothing
		}
		[Lua([Security.Capability.None])]
		public virtual void AddTag(string tag)
		{
			lock (Tags)
				if (!Tags.Contains(tag))
					Tags.Add(tag);
		}
		[Lua([Security.Capability.None])]
		public virtual Instance? Clone()
		{
			lock (this)
			{
				if (!Archivable)
					return null;
				// i tried
				// maybe i did it
				Dictionary<Instance, Instance> clonemapping = [];
				List<Instance> dolater = [];

				Instance? DoClone(Instance? inst)
				{
					if (inst == null) return null;
					var clone = (Instance)Activator.CreateInstance(inst.GetType(), GameManager)!;
					var props = SerializationManager.GetAccessibleProperties(clone);
					for (int i = 0; i < props.Length; i++)
					{
						try
						{
							var prop = SerializationManager.GetProperty(inst, props[i]);
							var ptyp = SerializationManager.GetPropertyType(clone, props[i]);
							if (SerializationManager.IsReadonly(clone, props[i]))
								continue;
							if (ptyp.IsAssignableTo(typeof(Script))) continue;
							if (ptyp.IsAssignableTo(typeof(Instance)) && prop != null)
							{
								var ogval = (Instance)prop;
								if (clonemapping.TryGetValue(ogval, out Instance? value))
									SerializationManager.SetProperty(clone, props[i], value);
								else
									dolater.Add(clone);
							}
							else
								SerializationManager.SetProperty(clone, props[i], prop);
						}
						catch
						{
							// we dont care
						}
					}

					clonemapping[inst] = clone;

					for (int i = 0; i < inst.Children.Count; i++)
						if (inst.Children[i].Archivable)
						{
							var cl = DoClone(inst.Children[i]);
							if (cl == null) continue;
							cl.Parent = clone;
						}

					return clone;
				}

				for (int i = 0; i < dolater.Count; i++)
				{
					var inst = dolater[i];
					var props = SerializationManager.GetAccessibleProperties(inst);
					for (int j = 0; j < props.Length; j++)
					{
						var prop = SerializationManager.GetProperty(inst, props[j]);
						var ptyp = SerializationManager.GetPropertyType(inst, props[j]);
						if (SerializationManager.IsReadonly(inst, props[j]))
							continue;
						if (ptyp.IsAssignableTo(typeof(Instance)) && prop != null)
						{
							var ogval = (Instance)prop;
							SerializationManager.SetProperty(inst, props[j], clonemapping[ogval]); // i HOPE that every inst reference will be resolved this way
						}
					}
				}

				return DoClone(this);
			}
		}
		public virtual Instance ForceClone()
		{
			// i tried
			// maybe i did it
			lock (this)
			{
				Dictionary<Instance, Instance> clonemapping = [];
				List<Instance> dolater = [];

				Instance DoClone(Instance inst)
				{
					var clone = (Instance)Activator.CreateInstance(inst.GetType(), GameManager)!;
					var props = SerializationManager.GetAccessibleProperties(clone);
					for (int i = 0; i < props.Length; i++)
					{
						try
						{
							var prop = SerializationManager.GetProperty(inst, props[i]);
							var ptyp = SerializationManager.GetPropertyType(clone, props[i]);
							if (SerializationManager.IsReadonly(clone, props[i]))
								continue;
							if (ptyp.IsAssignableTo(typeof(LuaSignal)))
								continue;
							if (ptyp.IsAssignableTo(typeof(Instance)) && prop != null)
							{
								var ogval = (Instance)prop;
								if (clonemapping.TryGetValue(ogval, out Instance? value))
									SerializationManager.SetProperty(clone, props[i], value);
								else
									dolater.Add(clone);
							}
							else
								SerializationManager.SetProperty(clone, props[i], prop);
						}
						catch
						{
							// we dont care
						}
					}

					clonemapping[inst] = clone;

					for (int i = 0; i < inst.Children.Count; i++)
						DoClone(inst.Children[i]).Parent = clone;

					return clone;
				}

				for (int i = 0; i < dolater.Count; i++)
				{
					var inst = dolater[i];
					var props = SerializationManager.GetAccessibleProperties(inst);
					for (int j = 0; j < props.Length; j++)
					{
						var prop = SerializationManager.GetProperty(inst, props[j]);
						var ptyp = SerializationManager.GetPropertyType(inst, props[j]);
						if (SerializationManager.IsReadonly(inst, props[j]))
							continue;
						if (ptyp.IsAssignableTo(typeof(Instance)) && prop != null && ptyp.Name != "Parent")
						{
							var ogval = (Instance)prop;
							SerializationManager.SetProperty(inst, props[j], clonemapping[ogval]); // i HOPE that every inst reference will be resolved this way
						}
					}
				}

				return DoClone(this);
			}
		}
		[Lua([Security.Capability.None])]
		public virtual void ClearAllChildren()
		{
			lock (Children)
			{
				for (int i = 0; i < Children.Count; i++) Children[i].Destroy();
				Children.Clear();
			}
		}
		[Lua([Security.Capability.None])]
		public virtual void Destroy()
		{
			if (!WasDestroyed)
			{
				Destroying.Fire();

				// fck it im gonna delete all references old fashioned way
				for (int i = 0; i < GameManager.AllInstances.Count; i++)
				{
					var inst = GameManager.AllInstances[i];
					if (inst.containsInstanceReferences)
					{
						inst.ClearReferencesTo(this);
					}
				}

				Parent = null;
				ClearAllChildren();
				GameManager.AllInstances.Remove(this);

				WasDestroyed = true;

				if (GameManager.AllowReplication && GameManager.NetworkManager.IsServer)
					GameManager.NetworkManager.AddReplication(this, Replication.REPM_TOALL, Replication.REPW_DESTROY, false);
			}
		}
		[Lua([Security.Capability.None])]
		public virtual Instance? FindFirstAncestor(string name)
		{
			if (Parent == null) return null;
			lock (Parent)
			{
				return Parent.Name == name ? Parent : Parent.FindFirstAncestor(name);
			}
		}
		[Lua([Security.Capability.None])]
		public virtual Instance? FindFirstAncestorOfClass(string cl)
		{
			if (Parent == null) return null;
			lock (Parent)
			{
				return Parent.ClassName == cl ? Parent : Parent.FindFirstAncestorOfClass(cl);
			}
		}
		[Lua([Security.Capability.None])]
		public virtual Instance? FindFirstAncestorWhichIsA(string cl)
		{
			if (Parent == null) return null;
			lock (Parent)
			{
				return Parent.IsA(cl) ? Parent : Parent.FindFirstAncestorWhichIsA(cl);
			}
		}
		[Lua([Security.Capability.None])]
		public virtual Instance? FindFirstChild(string name)
		{
			lock (Children)
			{
				for (int i = 0; i < Children.Count; i++)
					if (Children[i].Name == name)
						return Children[i];
				return null;
			}
		}
		[Lua([Security.Capability.None])]
		public virtual Instance? FindFirstChildOfClass(string cl)
		{
			lock (Children)
			{
				for (int i = 0; i < Children.Count; i++)
					if (Children[i].ClassName == cl)
						return Children[i];

				return null;
			}
		}
		[Lua([Security.Capability.None])]
		public virtual Instance? FindFirstChildWhichIsA(string cl)
		{
			lock (Children)
			{
				for (int i = 0; i < Children.Count; i++)
					if (Children[i].IsA(cl))
						return Children[i];

				return null;
			}
		}
		[Lua([Security.Capability.None])]
		public virtual Instance? FindFirstDescendant(string name)
		{
			lock (Children)
			{
				for (int i = 0; i < Children.Count; i++)
					if (Children[i].Name == name)
						return Children[i];

				for (int i = 0; i < Children.Count; i++)
				{
					var child = Children[i];
					var descendant = child.FindFirstDescendant(name);
					if (descendant != null) return descendant;
				}

				return null;
			}
		}
		[Lua([Security.Capability.None])]
		public virtual LuaSignal GetPropertyChangedSignal(string prop)
		{
			lock (ChangedSignals)
			{
				if (!ChangedSignals.ContainsKey(prop))
					ChangedSignals[prop] = new(GameManager);
				return ChangedSignals[prop];
			}
		}
		[Lua([Security.Capability.None])]
		public virtual Instance[] GetChildren()
		{
			lock (Children) // that sounds interesting
				return [.. Children];
		}
		// poorly optimized
		public virtual T[] GetDescendantsOfType<T>()
		{
			lock (Children)
			{
				var list = new List<T>();

				for (int i = 0; i < Children.Count; i++)
				{
					if (Children[i] is T inst)
						list.Add(inst);
					list.AddRange(Children[i].GetDescendantsOfType<T>());
				}

				return [.. list];
			}
		}
		[Lua([Security.Capability.None])]
		public virtual Instance[] GetDescendants()
		{
			lock (Children)
			{
				var list = new List<Instance>(Children);

				for (int i = 0; i < Children.Count; i++)
					list.AddRange(Children[i].GetDescendants());

				return [.. list];
			}
		}
		[Lua([Security.Capability.None])]
		public virtual Instance[] GetAncestors()
		{
			if (Parent == null) return [];

			lock (Parent)
			{
				var list = new List<Instance>();
				var inst = Parent;

				while (inst != null)
				{
					list.Add(inst);
					inst = inst.Parent!;
				}

				return [.. list];
			}
		}
		[Lua([Security.Capability.None])]
		public virtual string GetFullName()
		{
			if (parent == null) return Name;

			var strings = new List<string>();
			var inst = Parent!;

			strings.Add(Name);

			while (inst != null && !inst.IsA("DataModel"))
			{
				strings.Add(inst.Name);
				inst = inst.Parent!;
			}

			strings.Reverse();
			return string.Join('.', strings);
		}
		[Lua([Security.Capability.None])]
		public virtual bool IsDescendantOf(Instance instance) => GetAncestors().Contains(instance);
		[Lua([Security.Capability.None])]
		public virtual bool IsAncestorOf(Instance instance) => GetDescendants().Contains(instance);
		[Lua([Security.Capability.None])]
		public virtual string[] GetTags() => [.. Tags];
		[Lua([Security.Capability.None])]
		public virtual bool HasTag(string tag) => Tags.Contains(tag);
		[Lua([Security.Capability.None])]
		public virtual void RemoveTag(string tag) => Tags.Remove(tag);
		[Lua([Security.Capability.None])]
		public virtual bool IsA(string classname) => nameof(Instance) == classname;
		public int CountDescendants()
		{
			lock (Children)
			{
				int sum = Children.Count;
				for (int i = 0; i < Children.Count; i++)
					sum += Children[i].CountDescendants();
				return sum;
			}
		}
		[Lua([Security.Capability.None])]
		public LuaYield WaitForChild(string name)
		{
			var job = TaskScheduler.CurrentJob;
			job.JobTimingContext.TaskJoinedTo = Task.Run(async () =>
			{
				while (!GameManager.ShuttingDown)
				{
					var ch = FindFirstChild(name);
					if (ch == null)
						await Task.Yield();
					else
					{
						job.ScriptJobContext.YieldReturn = [ LuaRuntime.PushInstance(ch) ];
						return;
					}
				}
			});
			return new();
		}
		[Lua([Security.Capability.CoreSecurity])]
		public void ClearReferencesTo(Instance inst)
		{
			var props = GetType().GetProperties();
			for (int i = 0; i < props.Length; i++)
			{
				var prop = props[i];
				if (prop.PropertyType.IsAssignableTo(NetworkManager.InstanceType))
				{
					if (prop.Name != "Parent")
					{
						var obj = prop.GetValue(this);
						if (obj == inst)
							prop.SetValue(this, null);
					}
				}
			}
		}
		public Task<Instance> WaitForChildInternal(string name)
		{
			return Task.Run(() =>
			{
				while (!GameManager.ShuttingDown)
				{
					var ch = FindFirstChild(name);
					if (ch == null)
						Thread.Yield();
					else
					{
						return ch;
					}
				}
				return null;
			});
		}
		public void ReplicateProperties(string[] props, bool immediate)
		{
			lock (this)
			{
				if (GameManager.NetworkManager.RemoteConnection != null || GameManager.NetworkManager.IsServer)
				{
					if (DateTime.UtcNow > DoNotReplicateUntil || immediate)
					{
						var rep = GameManager.NetworkManager.AddReplication(this, Replication.REPM_BUTOWNER, Replication.REPW_PROPCHG, false);

						if (rep != null)
						{
							rep.Properties = (from x in props select ThisType.GetProperty(x)).ToArray();
							DoNotReplicateUntil = DateTime.UtcNow.AddMilliseconds(1000 / GameManager.PropertyReplicationRate);
						}
					}
				}
			}
		}
		public static byte[] SerializeToNBIF(Instance inst)
		{
			using MemoryStream ms = new();
			using BinaryWriter bw = new(ms);

			var type = inst.GetType();

			bw.Write(inst.ClassName);
			bw.Write(inst.UniqueID.ToByteArray());
			bw.Write(inst.ParentID.ToByteArray());
			bw.Write(inst.Tags.Count);
			for (int i = 0; i < inst.Tags.Count; i++)
				bw.Write(inst.Tags[i]);

			var props = type.GetProperties();
			var goodprops = new List<PropertyInfo>();

			for (int i = 0; i < props.Length; i++)
			{
				var prop = props[i];
				if (prop.GetCustomAttribute<NotReplicatedAttribute>() != null)
					continue;
				if (!prop.CanWrite)
					continue;
				goodprops.Add(prop);
			}

			bw.Write(goodprops.Count);

			for (int i = 0; i < goodprops.Count; i++)
			{
				var prop = goodprops[i];
				var value = prop.GetValue(inst);
				var valuetype = value.GetType();
				var valbts = SerializationManager.NetworkSerializers[valuetype.FullName](value, inst.GameManager);

				bw.Write(prop.Name);
				bw.Write(valbts.Length);
				bw.Write(valbts);
			}

			bw.Write(inst.Children.Count);

			for (int i = 0; i < inst.Children.Count; i++)
				bw.Write(SerializeToNBIF(inst.Children[i]));

			return ms.ToArray();
		}
		private struct DelayedProperty
		{
			public PropertyInfo Property;
			public Instance Target;
			public byte[] ValueBytes;
		}
		public static Instance? DeserializeFromNBIF(GameManager gm, Stream stream)
		{
			List<DelayedProperty> delayedProperties = [];

			Instance? Deserialize()
			{
				string? classname = null;
				string? name = null;

				try
				{
					using BinaryReader br = new(stream);

					classname = br.ReadString();
					var uniqueid = br.ReadBytes(16);
					var parentid = br.ReadBytes(16);
					var tagcount = br.ReadInt32();
					var tags = new List<string>();

					for (int i = 0; i < tagcount; i++)
						tags.Add(br.ReadString());

					var insttype = InstanceCreator.InstanceTypes.First(x => x.Name == classname);
					var impersonation = insttype.GetCustomAttribute<ImpersonateDuringReplicationAttribute>();

					if (impersonation != null)
						Security.Impersonate(impersonation.Level);

					var inst = InstanceCreator.CreateInstanceIfExists(classname, gm);
					inst.Tags = tags;

					var propcount = br.ReadInt32();

					for (int i = 0; i < propcount; i++)
					{
						var propname = br.ReadString();
						var propvalcount = br.ReadInt32();
						var propvalbts = br.ReadBytes(propvalcount);
						var prop = insttype.GetProperty(propname);
						var propval = SerializationManager.NetworkDeserializers[propname](propvalbts, gm);

						if (propval == null)
						{
							delayedProperties.Add(new()
							{
								Property = prop,
								Target = inst,
								ValueBytes = propvalbts
							});
							continue;
						}
						if (propname == "Name")
							name = propval as string;

						prop.SetValue(inst, propval);
					}

					var childcount = br.ReadInt32();

					for (int i = 0; i < childcount; i++)
					{
						Instance? child = Deserialize();
						if (child == null)
							continue;
						child.Parent = inst;
					}

					if (impersonation != null)
						Security.EndImpersonate();

					return inst;
				}
				catch (Exception ex)
				{
					LogManager.LogError($"Failed to deserialize {classname ?? "<unk>"} - {name ?? "<unk>"} - {ex.GetType()} - {ex.Message}!");
					return null;
				}
			}

			for (int i = 0; i < delayedProperties.Count; i++)
			{
				var delayed = delayedProperties[i];
				var insttype = delayed.Target.GetType();
				var instprop = delayed.Property;
				var value = SerializationManager.NetworkDeserializers[instprop.PropertyType.FullName](delayed.ValueBytes, gm);

				instprop.SetValue(delayed.Target, value);
			}

			return Deserialize();
		}
	}
}
