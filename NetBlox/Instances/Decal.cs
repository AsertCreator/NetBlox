using NetBlox.Rendering;
using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;

namespace NetBlox.Instances;

[Creatable]
[ExplorerIconIndex(7)]
public class Decal : FaceInstance
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public Color3 Color3 { get; set; } = Color.White;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public float Transparency { get; set; }
    [LuaAlias(Name = "Texture")]
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public string Content { get; set; } = "";

    public override string ClassName => nameof(Decal);

    public Decal(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(Decal))
            return base.IsA(className);
        return true;
    }
}