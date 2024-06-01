namespace NetBlox.Runtime
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
	public class LuaAttribute : Attribute
	{
		public Security.Capability[] Capabilities { get; set; } = [];

		public LuaAttribute(Security.Capability[] caps) 
		{ 
			Capabilities = caps;
		}
		public LuaSpace Space { get; set; } = LuaSpace.Both;
	}
	public enum LuaSpace
	{
		ClientOnly, ServerOnly, Both
	}
}
