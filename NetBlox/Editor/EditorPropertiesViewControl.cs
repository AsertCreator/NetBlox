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

    private float currentHeight = 0;
    private float propertyLineMargin = 5;
    private Vector2 absoluteSize = default;
    private Vector2 absoluteOffset = default;

    private int stringEditorCursorBegin = 0;
    private string? stringEditorCurrentValue = null;

    private InstanceBridge.InstanceClassCacheProperty? currentEditedProperty;

    private void RenderPropertyLine(
        InstanceBridge.InstanceClassCacheProperty property, Font font, FontSpecification fontSpecification, Instance instance)
    {
        Vector2 mousePosition = Raylib.GetMousePosition();
        Vector2 propertyEditPosition = new Vector2(absoluteSize.X / 2, currentHeight - propertyLineMargin / 2) + absoluteOffset;
        Vector2 propertyEditSize = new Vector2(absoluteSize.X / 2, propertyLineMargin + fontSpecification.Size);
        bool lockFocus = false;

        if (!property.IsReadOnly)
            Raylib.DrawTextEx(
                font, property.Name, new Vector2(fontSpecification.Size, currentHeight) + absoluteOffset, fontSpecification.Size, 0, Color.White);
        else
            Raylib.DrawTextEx(
                font, property.Name, new Vector2(fontSpecification.Size, currentHeight) + absoluteOffset, fontSpecification.Size, 0, Color.Gray);

        if (property.PropertyType == typeof(string))
        {
            RenderPropertyEditorString(property, font, fontSpecification, instance);

            if (property == currentEditedProperty)
            {
                Raylib.DrawRectangleLines(
                    (int)propertyEditPosition.X, (int)propertyEditPosition.Y, (int)propertyEditSize.X, (int)propertyEditSize.Y, Color.SkyBlue);
            }

            lockFocus = true;
        }
        else if (property.PropertyType == typeof(bool))
        {
            RenderPropertyEditorBoolean(property, font, fontSpecification, instance);
        }
        else
        {
            // as in disabled
            Raylib.DrawTextEx(
                font, property.Name, new Vector2(fontSpecification.Size, currentHeight) + absoluteOffset, fontSpecification.Size, 0, Color.Gray);
        }

        if (mousePosition.X > propertyEditPosition.X && mousePosition.X < propertyEditPosition.X + propertyEditSize.X &&
            mousePosition.Y > propertyEditPosition.Y && mousePosition.Y < propertyEditPosition.Y + propertyEditSize.Y &&
            Raylib.IsMouseButtonPressed(MouseButton.Left) && lockFocus)
        {
            currentEditedProperty = property;
            stringEditorCursorBegin = -1;
            stringEditorCurrentValue = null;
            instance.Root.GetService<CoreEditorService>().KeyboardFocused = true;
        }

        Raylib.DrawLine(
            (int)absoluteOffset.X, (int)(currentHeight + fontSpecification.Size + propertyLineMargin / 2 + absoluteOffset.Y), 
            (int)(absoluteSize.X + absoluteOffset.X), (int)(currentHeight + fontSpecification.Size + propertyLineMargin / 2 + absoluteOffset.Y), Color.Gray);
    }

    // this is the worst code in history

    private void RenderPropertyEditorBoolean(
        InstanceBridge.InstanceClassCacheProperty property, Font font, FontSpecification fontSpecification, Instance instance)
    {
        Vector2 mousePosition = Raylib.GetMousePosition();
        Vector2 propertyEditPosition = new Vector2(absoluteSize.X / 2, currentHeight - propertyLineMargin / 2) + absoluteOffset;
        Vector2 propertyEditSize = new Vector2(absoluteSize.X / 2, propertyLineMargin + fontSpecification.Size);
        CoreEditorService service = instance.Root.GetService<CoreEditorService>();
        bool value = (bool)property.Property.GetValue(instance)!;

        if (!property.IsReadOnly)
        {
            Raylib.DrawTextEx(font, value ? "Yes" : "No",
                new Vector2(2 + absoluteSize.X / 2, currentHeight) + absoluteOffset, fontSpecification.Size, 0, value ? Color.Green : Color.Red);
                
            if (mousePosition.X > propertyEditPosition.X && mousePosition.X < propertyEditPosition.X + propertyEditSize.X &&
                mousePosition.Y > propertyEditPosition.Y && mousePosition.Y < propertyEditPosition.Y + propertyEditSize.Y &&
                Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                property.Property.SetValue(instance, !value);
            }
        }
        else
        {
            Raylib.DrawTextEx(font, value ? "Yes" : "No",
                new Vector2(2 + absoluteSize.X / 2, currentHeight) + absoluteOffset, fontSpecification.Size, 0, value ? Color.DarkGreen : Color.Maroon);
        }
    }

    private void RenderPropertyEditorString(
        InstanceBridge.InstanceClassCacheProperty property, Font font, FontSpecification fontSpecification, Instance instance)
    {
        CoreEditorService service = instance.Root.GetService<CoreEditorService>();
        string? value = property.Property.GetValue(instance) as string;

        value ??= "";

        value = value.Replace("\n", "\\n");

        if (!property.IsReadOnly)
        {   
            if (property == currentEditedProperty && service.KeyboardFocused)
            {
                if (stringEditorCursorBegin == -1)
                    stringEditorCursorBegin = value.Length;
                if (stringEditorCurrentValue == null)
                    stringEditorCurrentValue = value;

                float x = Raylib.MeasureTextEx(font, stringEditorCurrentValue.Substring(0, stringEditorCursorBegin), fontSpecification.Size, 0).X;
                Vector2 position = new Vector2(2 + absoluteSize.X / 2, currentHeight) + absoluteOffset + new Vector2(x, 0);

                Raylib.DrawRectangle((int)position.X, (int)position.Y, (int)2, (int)fontSpecification.Size, Color.White);

                if (Raylib.IsKeyPressed(KeyboardKey.Left))
                {
                    stringEditorCursorBegin--;
                    if (stringEditorCursorBegin < 0)
                        stringEditorCursorBegin = 0;
                }
                else if (Raylib.IsKeyPressed(KeyboardKey.Right))
                {
                    stringEditorCursorBegin++;
                    if (stringEditorCursorBegin > stringEditorCurrentValue.Length)
                        stringEditorCursorBegin = stringEditorCurrentValue.Length;
                }
                else if (Raylib.IsKeyPressed(KeyboardKey.Backspace))
                {
                    if (stringEditorCursorBegin > 0)
                    {
                        stringEditorCurrentValue = 
                            stringEditorCurrentValue.Substring(0, stringEditorCursorBegin - 1) + stringEditorCurrentValue.Substring(stringEditorCursorBegin);
                        stringEditorCursorBegin--;
                    }
                }
                else if (Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsKeyPressed(KeyboardKey.Enter))
                {
                    currentEditedProperty = null;
                    stringEditorCurrentValue = stringEditorCurrentValue.Replace("\\n", "\n");
                    property.Property.SetValue(instance, stringEditorCurrentValue);
                    stringEditorCurrentValue = null;
                    service.KeyboardFocused = false;
                }

                char ch = (char)Raylib.GetCharPressed();
                while (ch > 0)
                {
                    stringEditorCurrentValue = stringEditorCurrentValue!.Substring(0, stringEditorCursorBegin) + ch + stringEditorCurrentValue.Substring(stringEditorCursorBegin);
                    stringEditorCursorBegin++;
                    ch = (char)Raylib.GetCharPressed();
                }

                Raylib.DrawTextEx(font, stringEditorCurrentValue,
                    new Vector2(2 + absoluteSize.X / 2, currentHeight) + absoluteOffset, fontSpecification.Size, 0, Color.White);
            }
            else
            {
                Raylib.DrawTextEx(font, value,
                    new Vector2(2 + absoluteSize.X / 2, currentHeight) + absoluteOffset, fontSpecification.Size, 0, Color.White);
            }
        }
        else
        {
            Raylib.DrawTextEx(font, value,
                new Vector2(2 + absoluteSize.X / 2, currentHeight) + absoluteOffset, fontSpecification.Size, 0, Color.Gray);
        }
    }

    private void HandleScrolling()
    {
        Vector2 mousePosition = Raylib.GetMousePosition();
        Vector2 actualMousewheelMovE = Raylib.GetMouseWheelMoveV();

        if (mousePosition.X > absoluteOffset.X && mousePosition.X < absoluteOffset.X + absoluteSize.X &&
            mousePosition.Y > absoluteOffset.Y && mousePosition.Y < absoluteOffset.Y + absoluteSize.Y)
        {
            ScrollOffset -= actualMousewheelMovE.Y * Raylib.GetWindowScaleDPI().Y * 3;   
            if (ScrollOffset < 0)
                ScrollOffset = 0;
            if (ScrollOffset > currentHeight - absoluteSize.Y)
                ScrollOffset = currentHeight - absoluteSize.Y;
            if (currentHeight < absoluteSize.Y)
                ScrollOffset = 0;
        }
    }

    public override void Render(CoreEditorService service, EditorWindow window, Vector2 offset, Vector2 windowSize)
    {
        FontSpecification fontSpecification = service.GameManager.GameRenderer!.DefaultFontSpecification;
        Font font = service.GameManager.GameRenderer.FontRegistry.LoadFontFromSpecification(fontSpecification);

        absoluteSize = Size.Resolve(windowSize);
        absoluteOffset = offset;

        Vector2 mousePosition = Raylib.GetMousePosition();

        Instance[] selection = service.Root.GetService<Selection>().Get();

        currentHeight = 0;

        if (selection.Length == 1)
        {
            Instance instance = selection[0];
            ScriptContext scriptContext = service.Root.GetService<ScriptContext>();
            InstanceBridge.InstanceClassCache classCache = scriptContext.ResolveInstanceClassCacheForType(instance.GetType());

            InstanceBridge.InstanceClassCacheProperty[] properties = classCache.GetStudioProperties();

            Raylib.DrawTextEx(font, "Property", new Vector2(fontSpecification.Size, currentHeight) + offset, fontSpecification.Size, 0, Color.Gray);
            Raylib.DrawTextEx(font, "Value", new Vector2(2 + absoluteSize.X / 2, currentHeight) + offset, fontSpecification.Size, 0, Color.Gray);
            Raylib.DrawLine(
                (int)offset.X, (int)(currentHeight + fontSpecification.Size + propertyLineMargin / 2 + offset.Y), 
                (int)(absoluteSize.X + offset.X), (int)(currentHeight + fontSpecification.Size + propertyLineMargin / 2 + offset.Y), Color.Gray);
            
            currentHeight += fontSpecification.Size + propertyLineMargin;

            foreach (InstanceBridge.InstanceClassCacheProperty property in properties)
            {
                RenderPropertyLine(property, font, fontSpecification, instance);

                currentHeight += fontSpecification.Size + propertyLineMargin;
            }

            Raylib.DrawLine((int)(absoluteSize.X / 2), (int)offset.Y, (int)(absoluteSize.X / 2), (int)(offset.Y + currentHeight - propertyLineMargin / 2), Color.Gray);
        }

        HandleScrolling();
    }
}