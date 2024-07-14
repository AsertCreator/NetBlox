using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace NetBlox.Instances
{
	public static class InstanceCreator
	{
		public static Type[] InstanceTypes;
		public static Type[] CreatableInstanceTypes;

		static InstanceCreator()
		{
			var it = typeof(Instance);
			InstanceTypes = (from x in Assembly.GetExecutingAssembly().GetTypes() where x.IsAssignableTo(it) select x).ToArray();
			CreatableInstanceTypes = (from x in InstanceTypes where x.GetCustomAttribute<CreatableAttribute>() != null select x).ToArray();
		}
		public static Instance CreateInstance(string cn, GameManager gm) => (Instance)Activator.CreateInstance((from x in InstanceTypes where x.Name == cn select x).First(), gm)!;
		public static Instance? CreateInstanceIfExists(string cn, GameManager gm) 
		{
			var inst = (from x in InstanceTypes where x.Name == cn select x).FirstOrDefault();
			if (inst == null) return null;
			return (Instance)Activator.CreateInstance(inst, gm)!; 
		}
		public static Instance CreateReplicatedInstance(string cn, GameManager gm)
		{
			var og = gm.AllowReplication;
			gm.AllowReplication = false;
			var inst = (Instance)Activator.CreateInstance((from x in InstanceTypes where x.Name == cn select x).First(), gm)!;
			gm.AllowReplication = og;
			return inst;
		}
		public static Instance CreateAccessibleInstance(string cn, GameManager gm)
		{
			return (Instance)Activator.CreateInstance((from x in InstanceTypes where 
													   x.Name == cn && x.GetCustomAttribute<CreatableAttribute>() != null select x).First(), gm)!;
		}
	}
}
