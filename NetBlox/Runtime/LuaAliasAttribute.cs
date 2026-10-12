namespace NetBlox.Runtime;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
public class LuaAliasAttribute : Attribute
{
    public required string Name { get; set; }
}