using System.Numerics;
using NetBlox.Instances.Services.Internal;
using NetBlox.Structs;

namespace NetBlox.Editor;

public abstract class EditorControl
{
    public UDim2 Position { get; set; }
    public UDim2 Size { get; set; }

    public CoreEditorService EditorService = null!;

    public abstract void Render(EditorWindow window, Vector2 offset, Vector2 windowSize);
}