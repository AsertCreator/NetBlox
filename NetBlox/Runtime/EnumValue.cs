namespace NetBlox.Runtime;

public record struct EnumValue
{
    public required Type EnumType;
    public required string Name;
    public required int Value;
}