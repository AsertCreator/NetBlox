namespace NetBlox.Runtime
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class LuaAttribute : Attribute
    {
        public LuaSpace Space { get; set; } = LuaSpace.Both;
    }
    public enum LuaSpace
    {
        ClientOnly, ServerOnly, Both
    }
}
