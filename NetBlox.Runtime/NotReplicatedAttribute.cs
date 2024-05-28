using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Runtime
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property)]
	public class NotReplicatedAttribute : Attribute
	{
	}
}
