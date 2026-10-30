namespace NetBlox.Runtime;

[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public class ExplorerIconIndexAttribute(int index) : Attribute
{
    public int Index { get; set; } = index;
}