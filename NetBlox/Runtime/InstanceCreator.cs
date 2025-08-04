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
		public static Type[] ServiceInstanceTypes;
		public static Type[] CreatableInstanceTypes;
		public static Type[] InstancesWithInstanceReferences;

		static InstanceCreator()
		{
			var it = typeof(Instance);
			InstanceTypes = (from x in Assembly.GetExecutingAssembly().GetTypes() where x.IsAssignableTo(it) select x).ToArray();
			ServiceInstanceTypes = (from x in InstanceTypes where x.GetCustomAttribute<ServiceAttribute>() != null select x).ToArray();
			CreatableInstanceTypes = (from x in InstanceTypes where x.GetCustomAttribute<CreatableAttribute>() != null select x).ToArray();
			InstancesWithInstanceReferences =
				(from x
				 in InstanceTypes
				 where x.FindMembers(
					 MemberTypes.Property,
					 BindingFlags.Instance,
					 (x, y) => (x as PropertyInfo).PropertyType.IsAssignableTo(it) && x.Name != "Parent", null).Length > 0
				 select x).ToArray();
		}
		public static Instance? CreateInstanceIfExists(string cn, GameManager gm) 
		{
			var inst = (from x in InstanceTypes where x.Name == cn select x).FirstOrDefault();
			if (inst == null) return null;
			return (Instance)Activator.CreateInstance(inst, gm)!; 
		}
		public static Instance? CreateAccessibleInstanceIfExists(string cn, GameManager gm)
		{
			var inst = (from x in CreatableInstanceTypes where x.Name == cn select x).FirstOrDefault();
			if (inst == null) return null;
			return (Instance)Activator.CreateInstance(inst, gm)!;
		}
		public static Instance? CreateServiceInstanceIfExists(string cn, GameManager gm)
		{
			var inst = (from x in ServiceInstanceTypes where x.Name == cn select x).FirstOrDefault();
			if (inst == null) return null;
			return (Instance)Activator.CreateInstance(inst, gm)!;
		}
		public static Instance CreateReplicatedInstance(string cn, GameManager gm)
		{
			Security.Impersonate(8);
			var og = gm.AllowReplication;
			gm.AllowReplication = false;
			var inst = (Instance)Activator.CreateInstance((from x in InstanceTypes where x.Name == cn select x).First(), gm)!;
			gm.AllowReplication = og;
			Security.EndImpersonate();
			return inst;
		}
	}
}
