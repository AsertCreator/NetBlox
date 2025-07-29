using MoonSharp.Interpreter;
using NetBlox.Runtime;
using NetBlox.Network;
using System.Diagnostics;

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
								parent.ChildRemoved.Fire(LuaRuntime.PushInstance(this));
								RaiseDescendantRemoved(this);
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
								value.ChildAdded.Fire(LuaRuntime.PushInstance(this));
								RaiseDescendantAdded(this);
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
		public LuaSignal DescendantAdded { get; init; } = new();
		[Lua([Security.Capability.None])]
		[NotReplicated]
		public LuaSignal DescendantRemoved { get; init; } = new();
		[Lua([Security.Capability.None])]
		[NotReplicated]
		public LuaSignal ChildAdded { get; init; } = new();
		[Lua([Security.Capability.None])]
		[NotReplicated]
		public LuaSignal ChildRemoved { get; init; } = new();
		[Lua([Security.Capability.None])]
		[NotReplicated]
		public LuaSignal Changed { get; init; } = new();
		[Lua([Security.Capability.None])]
		[NotReplicated]
		public LuaSignal Destroying { get; init; } = new();
		public virtual Security.Capability[] RequiredCapabilities => [];
		public bool WasDestroyed = false;
		public bool WasReplicated = false;
		public bool IsDomestic = false;
		public RemoteClient? Owner;
		public GameManager GameManager;
		public List<Instance> Children = [];
		public DateTime DestroyAt = DateTime.MaxValue;
		public DateTime DoNotReplicateUntil = DateTime.MinValue;
		public Dictionary<string, LuaSignal> ChangedSignals = [];
		public static Dictionary<int, Table> MetaTables = [];
		public Table? Table;
		private Instance? parent;
		private Type? ThisType;
		protected DataModel Root => GameManager.CurrentRoot;

		public Instance(GameManager gm)
		{
			lock (this)
			{
				Name = ClassName;
				UniqueID = Guid.NewGuid();
				GameManager = gm;

				gm.AllInstances.Add(this);
			}
			ThisType = GetType();
		}
		public void RaiseDescendantAdded(Instance descendantInQuestion) // not anymore
		{
			if (Parent != null)
			{
				Parent.DescendantAdded.Fire(LuaRuntime.PushInstance(descendantInQuestion));
				Parent.RaiseDescendantAdded(descendantInQuestion);
			}
		}
		public void RaiseDescendantRemoved(Instance descendantInQuestion) // not anymore
		{
			if (Parent != null)
			{
				Parent.DescendantRemoved.Fire(LuaRuntime.PushInstance(descendantInQuestion));
				Parent.RaiseDescendantRemoved(descendantInQuestion);
			}
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

				Parent = null;
				ClearAllChildren();
				GameManager.AllInstances.Remove(this);

				WasDestroyed = true;

				if (GameManager.AllowReplication)
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
					ChangedSignals[prop] = new();
				return ChangedSignals[prop];
			}
		}
		[Lua([Security.Capability.None])]
		public virtual Instance[] GetChildren()
		{
			lock (Children) // that sounds interesting
				return [.. Children];
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
		public virtual void SetNetworkOwner(Player player)
		{
			lock (this)
			{
				if (!GameManager.NetworkManager.IsServer)
					throw new ScriptRuntimeException("Cannot call Network Ownership API from client!");
				Debug.Assert(player.Client != null);

				var prevowner = Owner != null ? Owner.Player : null;
				var newowner = player;

				if (prevowner != null)
					prevowner.Client.SendPacket(NPUpdatePlayerOwnership.Create(this, false));
				else
					IsDomestic = false;
				if (newowner != null)
					newowner.Client.SendPacket(NPUpdatePlayerOwnership.Create(this, true));
				else
					IsDomestic = true;

				Owner = newowner.Client;

				OnNetworkOwnershipChanged();

				for (int i = 0; i < Children.Count; i++)
				{
					Children[i].SetNetworkOwner(player);
				}
			}
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
		private void ChangeOwnershipImpl(GameManager gm)
		{
			GameManager.AllInstances.Remove(this);
			Owner = null;
			IsDomestic = false;
			GameManager = gm;
			WasReplicated = false;
			WasDestroyed = false;
			GameManager.AllInstances.Add(this);

			for (int i = 0; i < Children.Count; i++)
			{
				Children[i].ChangeOwnershipImpl(gm);
			}
		}
		public void ChangeOwnership(GameManager gm)
		{
			Parent = null;
			ChangeOwnershipImpl(gm);
		}
		public virtual void OnNetworkOwnershipChanged() { }
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
			job.JobTimingContext.TaskJoinedTo = Task.Run(() =>
			{
				while (!GameManager.ShuttingDown)
				{
					var ch = FindFirstChild(name);
					if (ch == null)
						Thread.Sleep(50);
					else
					{
						job.ScriptJobContext.YieldReturn = [ LuaRuntime.PushInstance(ch) ];
						return;
					}
				}
			});
			return new();
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
	}
}
