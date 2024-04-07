using MoonSharp.Interpreter;
using NetBlox.Runtime;
using System.Text.Json.Serialization;

namespace NetBlox.Instances
{
	public class Instance
	{
		[Lua]
		public bool Archivable { get; set; }
		[Lua]
		public string ClassName => GetType().Name;
		[Lua]
		public string Name { get; set; }
		[Lua]
		[JsonIgnore]
		public Instance? Parent
		{
			get => parent;
			set
			{
				if (WasDestroyed) return;

				if (parent != null)
					parent.Children.Remove(this);
				if (value != null)
				{
					parent = value;
					ParentID = parent.UniqueID;
					value.Children.Add(this);
				}
				else
				{
					parent = null;
					ParentID = Guid.Empty;
				}
			}
		}
		[JsonIgnore]
		public List<string> Tags { get; set; } = new();
		public Guid ParentID { get; set; }
		public Guid UniqueID { get; set; }
		[JsonIgnore]
		public bool WasDestroyed = false;
		[JsonIgnore]
		public bool WasReplicated = false;
		[JsonIgnore]
		public List<Instance> Children = new();
		[JsonIgnore]
		public Table? LuaTable;
		private Instance? parent;

		public Instance()
		{
			Name = ClassName;
			UniqueID = Guid.NewGuid();

			GameManager.InvokeAddedEvent(this);
			GameManager.AllInstances.Add(this);
		}
		public Instance(Guid guid)
		{
			Name = ClassName;
			UniqueID = guid;
			WasReplicated = true;

			GameManager.InvokeAddedEvent(this);
			GameManager.AllInstances.Add(this);
		}

		public virtual void Process()
		{
			// process nothing
		}
		public virtual void RenderUI()
		{
			// render nothing
		}
		[Lua]
		public virtual void AddTag(string tag)
		{
			if (!Tags.Contains(tag))
				Tags.Add(tag);
		}
		[Lua]
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
		[Lua]
		public virtual void ClearAllChildren()
		{
			for (int i = 0; i < Children.Count; i++) Children[i].Destroy();
			Children.Clear();
		}
		[Lua]
		public virtual void Destroy()
		{
			if (!WasDestroyed)
			{
				Parent = null;
				ClearAllChildren();
				GameManager.AllInstances.Remove(this);

				WasDestroyed = true;
			}
		}
		[Lua]
		public virtual Instance? FindFirstAncestor(string name)
		{
			if (Parent == null) return null;
			if (Parent.Name == name) return Parent;
			else return Parent.FindFirstAncestor(name);
		}
		[Lua]
		public virtual Instance? FindFirstAncestorOfClass(string cl)
		{
			if (Parent == null) return null;
			if (Parent.ClassName == cl) return Parent;
			else return Parent.FindFirstAncestorOfClass(cl);
		}
		[Lua]
		public virtual Instance? FindFirstAncestorWhichIsA(string cl)
		{
			if (Parent == null) return null;
			if (Parent.IsA(cl)) return Parent;
			else return Parent.FindFirstAncestorWhichIsA(cl);
		}
		[Lua]
		public virtual Instance? FindFirstChild(string name)
		{
			for (int i = 0; i < Children.Count; i++)
				if (Children[i].Name == name)
					return Children[i];

			return null;
		}
		[Lua]
		public virtual Instance? FindFirstChildOfClass(string cl)
		{
			for (int i = 0; i < Children.Count; i++)
				if (Children[i].ClassName == cl)
					return Children[i];

			return null;
		}
		[Lua]
		public virtual Instance? FindFirstChildWhichIsA(string cl)
		{
			for (int i = 0; i < Children.Count; i++)
				if (Children[i].IsA(cl))
					return Children[i];

			return null;
		}
		[Lua]
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
		[Lua]
		public virtual Instance[] GetChildren() => Children.ToArray();
		[Lua]
		public virtual Instance[] GetDescendants()
		{
			var list = new List<Instance>(Children);

			for (int i = 0; i < Children.Count; i++)
				list.AddRange(Children[i].GetDescendants());

			return list.ToArray();
		}
		[Lua]
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
		[Lua]
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
		[Lua]
		public virtual bool IsDescendantOf(Instance instance) => GetAncestors().Contains(instance);
		[Lua]
		public virtual bool IsAncestorOf(Instance instance) => GetDescendants().Contains(instance);
		[Lua]
		public virtual string[] GetTags() => Tags.ToArray();
		[Lua]
		public virtual bool HasTag(string tag) => Tags.Contains(tag);
		[Lua]
		public virtual void RemoveTag(string tag) => Tags.Remove(tag);
		[Lua]
		public virtual bool IsA(string classname) => nameof(Instance) == classname;
		public void ReplicateProps()
		{
			if (NetworkManager.ServerConnection != null)
				NetworkManager.SeqReplicateInstance(NetworkManager.ServerConnection, this, false);
		}
	}
}
