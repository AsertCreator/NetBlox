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

		static InstanceCreator()
		{
			var it = typeof(Instance);
			InstanceTypes = (from x in Assembly.GetExecutingAssembly().GetTypes() where x.IsAssignableTo(it) select x).ToArray();
		}
		public static Type GetInstanceType(string cn) => (from x in InstanceTypes where x.Name == cn select x).First()!;
		public static Instance CreateInstance(string cn) => (Instance)Activator.CreateInstance((from x in InstanceTypes where x.Name == cn select x).First())!;
		public static Instance CreateAccessibleInstance(string cn)
		{
			return (Instance)Activator.CreateInstance((from x in InstanceTypes where 
													   x.Name == cn && x.GetCustomAttribute<CreatableAttribute>() != null select x).First())!;
		}
	}
}
