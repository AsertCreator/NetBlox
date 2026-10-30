using System.Numerics;
using NetBlox.Instances.Services.Internal;
using NetBlox.Structs;
using Raylib_cs;

namespace NetBlox.Editor;

public class EditorWindow
{
    public UDim2 Position { get; set; }
    public UDim2 Size { get; set; }
    public string Title { get; set; } = "";
    public List<EditorControl> Children { get; set; } = [];

    public void RenderWindow(CoreEditorService coreEditorService)
    {
        Color windowBackgroundColor = new Color(30, 35, 40, 150);
        Vector2 container = new Vector2(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
        Vector2 position = Position.Resolve(container);
        Vector2 size = Size.Resolve(container);

        float fontsize = coreEditorService.GameManager.GameRenderer!.DefaultFontSpecification.Size;

        position += new Vector2(1, fontsize + 3);
        size -= new Vector2(2, fontsize + 4);

        Raylib.DrawRectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y, windowBackgroundColor);

        Raylib.BeginScissorMode((int)position.X, (int)position.Y, (int)size.X, (int)size.Y);

        foreach (EditorControl editorControl in Children)
        {
            editorControl.Render(coreEditorService, this, position, size);
        }

        Raylib.EndScissorMode();
    }
}