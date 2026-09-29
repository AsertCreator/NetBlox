using System.Numerics;
using NetBlox.Runtime;

namespace NetBlox.Instances.Parts;

[Creatable]
public class Model : PVInstance
{
    public override string ClassName => nameof(Model);

    public Model(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public void TranslateBy(Vector3 delta)
    {
        // how much in o() is this

        Instance[] instances = GetDescendants();
        for (int i = 0; i < instances.Length; i++)
        {
            BasePart? basePart = instances[i] as BasePart;
            if (basePart != null)
                basePart.Position += delta;
        }
    }

    public override bool IsA(string className)
    {
        if (className != nameof(Model))
            return base.IsA(className);
        return true;
    }
}