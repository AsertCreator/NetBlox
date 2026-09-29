namespace NetBlox.Runtime;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
public class ScriptCallableAttribute : Attribute
{
    public required SimpleSecurityCapabilityLevel RequiredLevel { get; init; }
    public SimpleSecurityCapabilityLevel SetValueLevel { get; init; }
}