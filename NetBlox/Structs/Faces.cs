namespace NetBlox.Structs;

[Flags]
public enum Faces : int
{
    Right = 1, Top = 2, Back = 4, Left = 8, Bottom = 16, Front = 32, 
    All = Left | Right | Front | Top | Bottom | Back
}