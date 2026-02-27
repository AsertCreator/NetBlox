using MoonSharp.Interpreter;
using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace NetBlox.Instances
{
	public class ServiceProvider : Instance
	{
		private static readonly Type ServiceTypeType = typeof(ServiceType);
		private static readonly ServiceType[] ServiceTypeTypeValues = ServiceTypeType.GetEnumValues() as ServiceType[];
		private static readonly string[] ServiceTypeTypeNames = ServiceTypeType.GetEnumNames();

		public ServiceProvider(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(ServiceProvider) == classname) return true;
			return base.IsA(classname);
		}
		public T GetService<T>(bool allownull = false) where T : Instance
		{
			ServiceType[] types = ServiceTypeTypeValues;
			string[] typestrings = ServiceTypeTypeNames;
			string needlestring = typeof(T).Name;
			int index = Array.IndexOf(typestrings, needlestring);

			if (index == -1)
			{
				throw new Exception(typeof(T).Name + " is not a service!");
			}

			ServiceType servicetype = types[index];
			Instance? service = GameManager.TryGetService(servicetype);

			if (service != null)
			{
				service.Parent = this;
				return service as T;
			}

			return GameManager.CreateService(servicetype) as T;
		}
		[Lua([Security.Capability.None])]
		public Instance GetService(string sn)
		{
			ServiceType[] types = ServiceTypeTypeValues;
			string[] typestrings = ServiceTypeTypeNames;
			int index = Array.IndexOf(typestrings, sn);

			if (index == -1)
			{
				throw new Exception(sn + " is not a service!");
			}

			ServiceType servicetype = types[index];
			Instance? service = GameManager.TryGetService(servicetype);

			if (service != null)
			{
				service.Parent = this;
				return service;
			}

			return GameManager.CreateService(servicetype);
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
