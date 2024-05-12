using MoonSharp.Interpreter;
using NetBlox.Runtime;
using System.Text.Json.Serialization;

namespace NetBlox.Instances
{
	public class Instance
	{
		[Lua([Security.Capability.None])]
		public bool Archivable { get; set; }
        [Lua([Security.Capability.None])]
        public string ClassName => GetType().Name;
        [Lua([Security.Capability.None])]
        public string Name { get; set; }
        [Lua([Security.Capability.None])]
        [NotReplicated]
		public Instance? Parent
		{
			get => parent;
			set
			{
				if (WasDestroyed) return;

				if (parent != null)
				{
					parent.Children.Remove(this);
                    if (GameManager.CurrentRoot.MainEnv != null)
                        parent.ChildRemoved.Fire(DynValue.NewTable(LuaRuntime.MakeInstanceTable(this, GameManager.CurrentRoot.MainEnv)));
                }
				if (value != null)
				{
					parent = value;
					ParentID = parent.UniqueID;
					value.Children.Add(this);
					if (GameManager.CurrentRoot.MainEnv != null)
						value.ChildAdded.Fire(DynValue.NewTable(LuaRuntime.MakeInstanceTable(this, GameManager.CurrentRoot.MainEnv)));
                }
				else
				{
					parent = null;
					ParentID = Guid.Empty;
				}

				if (NetworkManager.IsServer)
					for (int i = 0; i < GameManager.AllClients.Count; i++)
					{
						NetworkManager.SeqReparentInstance(GameManager.AllClients[i].Connection, this);
					}
			}
		}
		[NotReplicated]
		public List<string> Tags { get; set; } = new();
		public Guid ParentID { get; set; }
		public Guid UniqueID { get; set; }
		[Lua([Security.Capability.None])]
        [NotReplicated]
        public LuaSignal ChildAdded { get; set; } = new();
        [Lua([Security.Capability.None])]
        [NotReplicated]
        public LuaSignal ChildRemoved { get; set; } = new();
        [Lua([Security.Capability.None])]
        [NotReplicated]
        public LuaSignal Destroying { get; set; } = new();
        public bool WasDestroyed = false;
		public bool WasReplicated = false;
		public List<Instance> Children = new();
		public Dictionary<Script, Table> Tables = new();
		private Instance? parent;

		public Instance()
		{
			Name = ClassName;
			UniqueID = Guid.NewGuid();

			GameManager.AllInstances.Add(this);
			GameManager.InvokeAddedEvent(this);
		}
		public Instance(Guid guid)
		{
			Name = ClassName;
			UniqueID = guid;
			WasReplicated = true;

			GameManager.AllInstances.Add(this);
			GameManager.InvokeAddedEvent(this);
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
			if (!Tags.Contains(tag))
				Tags.Add(tag);
		}
		[Lua([Security.Capability.None])]
		public virtual Instance Clone()
		{
			var clone = new Instance()
			{
				Name = Name,
				Parent = null!,
				Archivable = Archivable
			};

			for (int i = 0; i < Children.Count; i++)
				if (Children[i].Archivable)
					clone.Children.Add(Children[i].Clone());

			return clone;
		}
		[Lua([Security.Capability.None])]
		public virtual void ClearAllChildren()
		{
			for (int i = 0; i < Children.Count; i++) Children[i].Destroy();
			Children.Clear();
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
			}
		}
		[Lua([Security.Capability.None])]
		public virtual Instance? FindFirstAncestor(string name)
		{
			if (Parent == null) return null;
			if (Parent.Name == name) return Parent;
			else return Parent.FindFirstAncestor(name);
		}
		[Lua([Security.Capability.None])]
		public virtual Instance? FindFirstAncestorOfClass(string cl)
		{
			if (Parent == null) return null;
			if (Parent.ClassName == cl) return Parent;
			else return Parent.FindFirstAncestorOfClass(cl);
		}
		[Lua([Security.Capability.None])]
		public virtual Instance? FindFirstAncestorWhichIsA(string cl)
		{
			if (Parent == null) return null;
			if (Parent.IsA(cl)) return Parent;
			else return Parent.FindFirstAncestorWhichIsA(cl);
		}
		[Lua([Security.Capability.None])]
		public virtual Instance? FindFirstChild(string name)
		{
			for (int i = 0; i < Children.Count; i++)
				if (Children[i].Name == name)
					return Children[i];

			return null;
		}
		[Lua([Security.Capability.None])]
		public virtual Instance? FindFirstChildOfClass(string cl)
		{
			for (int i = 0; i < Children.Count; i++)
				if (Children[i].ClassName == cl)
					return Children[i];

			return null;
		}
		[Lua([Security.Capability.None])]
		public virtual Instance? FindFirstChildWhichIsA(string cl)
		{
			for (int i = 0; i < Children.Count; i++)
				if (Children[i].IsA(cl))
					return Children[i];

			return null;
		}
		[Lua([Security.Capability.None])]
		public virtual Instance? FindFirstDescendant(string name)
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
		[Lua([Security.Capability.None])]
		public virtual Instance[] GetChildren() => Children.ToArray();
		[Lua([Security.Capability.None])]
		public virtual Instance[] GetDescendants()
		{
			var list = new List<Instance>(Children);

			for (int i = 0; i < Children.Count; i++)
				list.AddRange(Children[i].GetDescendants());

			return list.ToArray();
		}
		[Lua([Security.Capability.None])]
		public virtual Instance[] GetAncestors()
		{
			if (Parent == null) return null!;

			var list = new List<Instance>();
			var inst = Parent;

			while (inst != null)
			{
				list.Add(inst);
				inst = inst.Parent!;
			}

			return list.ToArray();
		}
		[Lua([Security.Capability.None])]
		public virtual string GetFullName()
		{
			if (parent == null) return Name;

			var strings = new List<string>();
			var inst = Parent!;

			strings.Add(Name);

			while (inst != null)
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
		public virtual string[] GetTags() => Tags.ToArray();
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
		public LuaYield<Instance> WaitForChild(string name)
		{
			var n = new LuaYield<Instance>();

			for (int i = 0; i < Children.Count; i++)
			{
				if (name == Children[i].Name)
				{
					n.HasResult = true;
					n.Result = Children[i];
					return n;
				}
			}

			n.HasResult = false;
			n.Result = null;
			return n;
		}
		public void ReplicateProps()
		{
			if (NetworkManager.ServerConnection != null)
				NetworkManager.SeqReplicateInstance(NetworkManager.ServerConnection, this, false, false);
		}
	}
}
