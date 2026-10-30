using System.Numerics;
using NetBlox.Instances;
using NetBlox.Instances.Services;
using NetBlox.Instances.Services.Internal;
using NetBlox.Rendering;
using NetBlox.Runtime;
using NetBlox.Runtime.Bridges;
using Raylib_cs;

namespace NetBlox.Editor;

/// <summary>
/// The Properties window custom control for NetBlox Editor. Please don't use it anywhere else. It assumes it's the only in the properties window.
/// </summary>
public class EditorPropertiesViewControl : EditorControl
{
    public float ScrollOffset;
    public Dictionary<Instance, bool> InstanceState = [];
    public Dictionary<Type, ExplorerIconIndexAttribute> ExplorerIconIndexAttributeMap = [];

    public override void Render(CoreEditorService service, EditorWindow window, Vector2 offset, Vector2 windowSize)
    {
        FontSpecification fontSpecification = service.GameManager.GameRenderer!.DefaultFontSpecification;
        Font font = service.GameManager.GameRenderer.FontRegistry.LoadFontFromSpecification(fontSpecification);
        Vector2 actualMousewheelMovE = Raylib.GetMouseWheelMoveV();
        Vector2 size = Size.Resolve(windowSize);

        Vector2 mousePosition = Raylib.GetMousePosition();

        Instance[] selection = service.Root.GetService<Selection>().Get();

        const float propertyLineMargin = 5;

        float y = 0;

        if (selection.Length == 1)
        {
            Instance instance = selection[0];
            ScriptContext scriptContext = service.Root.GetService<ScriptContext>();
            InstanceBridge.InstanceClassCache classCache = scriptContext.ResolveInstanceClassCacheForType(instance.GetType());

            InstanceBridge.InstanceClassCacheProperty[] properties = classCache.GetStudioProperties();

            Raylib.DrawTextEx(font, "Property", new Vector2(fontSpecification.Size, y) + offset, fontSpecification.Size, 0, Color.Gray);
            Raylib.DrawTextEx(font, "Value", new Vector2(2 + size.X / 2, y) + offset, fontSpecification.Size, 0, Color.Gray);
            Raylib.DrawLine(
                (int)offset.X, (int)(y + fontSpecification.Size + propertyLineMargin / 2 + offset.Y), 
                (int)(size.X + offset.X), (int)(y + fontSpecification.Size + propertyLineMargin / 2 + offset.Y), Color.Gray);
            
            y += fontSpecification.Size + propertyLineMargin;

            foreach (InstanceBridge.InstanceClassCacheProperty property in properties)
            {
                object? value = property.Property.GetValue(instance);
                Raylib.DrawTextEx(font, property.Name, new Vector2(fontSpecification.Size, y) + offset, fontSpecification.Size, 0, Color.White);
                if (value != null)
                    Raylib.DrawTextEx(font, value.ToString(),
                        new Vector2(2 + size.X / 2, y) + offset, fontSpecification.Size, 0, Color.White);
                Raylib.DrawLine(
                    (int)offset.X, (int)(y + fontSpecification.Size + propertyLineMargin / 2 + offset.Y), 
                    (int)(size.X + offset.X), (int)(y + fontSpecification.Size + propertyLineMargin / 2 + offset.Y), Color.Gray);

                y += fontSpecification.Size + propertyLineMargin;
            }

            Raylib.DrawLine((int)(size.X / 2), (int)offset.Y, (int)(size.X / 2), (int)(offset.Y + y - propertyLineMargin / 2), Color.Gray);
        }

        if (mousePosition.X > offset.X && mousePosition.X < offset.X + size.X &&
            mousePosition.Y > offset.Y && mousePosition.Y < offset.Y + size.Y)
        {
            ScrollOffset -= actualMousewheelMovE.Y * Raylib.GetWindowScaleDPI().Y * 3;   
            if (ScrollOffset < 0)
                ScrollOffset = 0;
            if (ScrollOffset > y - size.Y)
                ScrollOffset = y - size.Y;
            if (y < size.Y)
                ScrollOffset = 0;
        }
    }
}