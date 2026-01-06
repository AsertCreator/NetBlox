using NetBlox.Instances;
using System.Reflection;

namespace NetBlox.Runtime
{
	public struct InstanceCrossReference
	{
		public Instance Referer;
		public PropertyInfo RefererProperty;

		public void NullOut()
		{
			RefererProperty.SetValue(Referer, null);
		}
	}
}
