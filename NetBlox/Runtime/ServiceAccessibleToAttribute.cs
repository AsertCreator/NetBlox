namespace NetBlox.Runtime;

[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public class ServiceAccessibleToAttribute : Attribute
{
    public required SimpleSecurityCapabilityLevel RequiredLevel { get; init; }
}