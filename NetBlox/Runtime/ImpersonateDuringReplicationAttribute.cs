using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Runtime
{
	[AttributeUsage(AttributeTargets.Class)]
	public class ImpersonateDuringReplicationAttribute : Attribute
	{
		public int Level { get; set; }
	}
}
