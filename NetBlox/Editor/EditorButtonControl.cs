using System.Numerics;
using NetBlox.Instances;
using NetBlox.Instances.Services.Internal;
using NetBlox.Rendering;
using Raylib_cs;

namespace NetBlox.Editor;

public delegate bool EditorButtonControlEnabledCallback();
public delegate void EditorButtonControlClickedCallback();

public class EditorButtonControl : EditorControl
{
    public EditorButtonControlEnabledCallback EnabledCallback = () => true;
    public EditorButtonControlClickedCallback ClickedCallback = () => {};
    public string Text = "";

    public override void Render(CoreEditorService service, EditorWindow window, Vector2 offset, Vector2 windowSize)
    {
        FontSpecification fontSpecification = service.GameManager.GameRenderer!.DefaultFontSpecification;
        Font font = service.GameManager.GameRenderer.FontRegistry.LoadFontFromSpecification(fontSpecification);
        Vector2 size = Size.Resolve(windowSize);
        Vector2 position = Position.Resolve(windowSize);
        Vector2 mousePosition = Raylib.GetMousePosition();

        Vector2 textSize = Raylib.MeasureTextEx(font, Text, fontSpecification.Size, 0);

        bool enabled = EnabledCallback();
        bool inside = mousePosition.X >= offset.X + position.X && mousePosition.X <= offset.X + position.X + size.X &&
                      mousePosition.Y >= offset.Y + position.Y && mousePosition.Y <= offset.Y + position.Y + size.Y;

        Raylib.DrawRectangle((int)(position.X + offset.X), (int)(position.Y + offset.Y), (int)size.X, (int)size.Y,
            enabled ? (inside ? Color.DarkBlue : Color.Blue) : Color.DarkBlue);

        Raylib.DrawTextEx(font, Text, position + offset + size / 2 - textSize / 2, fontSpecification.Size, 0, Color.White);

        if (mousePosition.X >= offset.X + position.X && mousePosition.X <= offset.X + position.X + size.X &&
            mousePosition.Y >= offset.Y + position.Y && mousePosition.Y <= offset.Y + position.Y + size.Y && enabled &&
            Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            ClickedCallback();
        }
    }
}