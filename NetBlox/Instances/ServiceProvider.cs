using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace NetBlox.Instances
{
	public class ServiceProvider : Instance
	{
		public ServiceProvider(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(ServiceProvider) == classname) return true;
			return base.IsA(classname);
		}
		public T GetService<T>(bool allownull = false) where T : Instance
		{
			for (int i = 0; i < Children.Count; i++)
			{
				if (Children[i] is T)
					return (T)Children[i];
			}
			if (!allownull)
			{
				var serv = (T)Activator.CreateInstance(typeof(T), GameManager);
				Debug.Assert(serv != null);
				serv.Parent = this;
				return serv;
			}
			return null!;
		}
		[Lua([Security.Capability.None])]
		public Instance GetService(string sn)
		{
			for (int i = 0; i < Children.Count; i++)
			{
				if (Children[i].ClassName == sn)
					return Children[i];
			}
			var serv = InstanceCreator.CreateInstance(sn, GameManager);
			serv.Parent = this;
			return serv;
		}
		[Lua([Security.Capability.None])]
		public Instance getService(string sn) => GetService(sn);
		[Lua([Security.Capability.None])]
		public Instance service(string sn) => GetService(sn);
		[Lua([Security.Capability.None])]
		public Instance FindService(string sn)
		{
			for (int i = 0; i < Children.Count; i++)
			{
				if (Children[i].ClassName == sn)
					return Children[i];
			}
			return null!;
		}
	}
}
