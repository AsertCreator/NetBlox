using System.Numerics;
using System.Reflection;
using NetBlox.Instances;
using NetBlox.Instances.Services.Internal;
using NetBlox.Rendering;
using NetBlox.Runtime;
using Raylib_cs;

namespace NetBlox.Editor;

/// <summary>
/// The Explorer window custom control for NetBlox Editor. Please don't use it anywhere else. It assumes it's the only in the explorer window.
/// </summary>
public class EditorExplorerViewControl : EditorControl
{
    public float ScrollOffset;
    public Dictionary<Instance, bool> InstanceState = [];
    public Dictionary<Type, ExplorerIconIndexAttribute> ExplorerIconIndexAttributeMap = [];

    private float currentHeight = 0;
    private float currentTabulation = 0;
    private float explorerLineMargin = 5;
    private Vector2 absoluteSize = default;
    private Vector2 absoluteOffset = default;

    private List<Instance> allInstances = [];

    private bool IsExpanded(Instance instance)
    {
        if (InstanceState.ContainsKey(instance))
            return InstanceState[instance];
        return false;
    }
    private void ToggleExpansion(Instance instance)
    {
        if (InstanceState.ContainsKey(instance))
            InstanceState[instance] = !InstanceState[instance];
        else
            InstanceState[instance] = true;
    }
    private void Collapse(Instance instance)
    {
        InstanceState[instance] = false;
    }
    private void Expand(Instance instance)
    {
        InstanceState[instance] = true;
    }

    private void RenderItem(Instance instance)
    {
        Instance[] selection = EditorService.Root.GetService<Selection>().Get();
        Vector2 completePosition = absoluteOffset + new Vector2(currentTabulation, currentHeight - ScrollOffset);

        FontSpecification fontSpecification = EditorService.GameManager.GameRenderer!.DefaultFontSpecification;
        Font font = EditorService.GameManager.GameRenderer.FontRegistry.LoadFontFromSpecification(fontSpecification);

        Vector2 mousePosition = Raylib.GetMousePosition();

        allInstances.Add(instance);

        if (completePosition.Y >= absoluteOffset.Y - fontSpecification.Size && completePosition.Y < absoluteOffset.Y + absoluteSize.Y)
        {
            if (selection.Contains(instance))
                Raylib.DrawRectangle((int)completePosition.X, (int)completePosition.Y, 
                    (int)absoluteSize.X, (int)(fontSpecification.Size + explorerLineMargin), Color.Blue);
            
            Raylib.DrawTextEx(font, instance.Name + " - " + instance.ClassName, 
                completePosition + new Vector2((fontSpecification.Size + explorerLineMargin) * 2 + 2, explorerLineMargin / 2),
                fontSpecification.Size, 0, Color.White);
            
            Type instanceType = instance.GetType();
            if (!ExplorerIconIndexAttributeMap.ContainsKey(instanceType))
                ExplorerIconIndexAttributeMap[instanceType] = instanceType.GetCustomAttribute<ExplorerIconIndexAttribute>()!;
            
            int explorerIconIndex = ExplorerIconIndexAttributeMap[instanceType].Index;

            Raylib.DrawTexturePro(EditorService.InstanceClassAtlas,
                new Rectangle(16 * explorerIconIndex, 0, 16, 16),
                new Rectangle(completePosition + 
                    new Vector2(fontSpecification.Size + explorerLineMargin + 1, (explorerLineMargin + fontSpecification.Size) / 2 - 8), 16, 16),
                default, 0, Color.White);
            
            if (instance.AliveChildrenCount > 0)
            {
                if (!InstanceState.ContainsKey(instance))
                    Raylib.DrawRectangle(
                        (int)(completePosition.X + explorerLineMargin), (int)(completePosition.Y + explorerLineMargin),
                        (int)(fontSpecification.Size - explorerLineMargin), (int)(fontSpecification.Size - explorerLineMargin),
                        Color.Red);
                else
                    Raylib.DrawRectangle(
                        (int)(completePosition.X + explorerLineMargin), (int)(completePosition.Y + explorerLineMargin),
                        (int)(fontSpecification.Size - explorerLineMargin), (int)(fontSpecification.Size - explorerLineMargin),
                        InstanceState[instance] ? Color.Green : Color.Red);
            }
        
            // handle Expand/Close button click

            if (mousePosition.X >= completePosition.X && mousePosition.X <= completePosition.X + fontSpecification.Size + explorerLineMargin &&
                mousePosition.Y >= completePosition.Y && mousePosition.Y <= completePosition.Y + fontSpecification.Size + explorerLineMargin &&
                Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                ToggleExpansion(instance);
            }
            if (mousePosition.X > completePosition.X + fontSpecification.Size + explorerLineMargin &&
                mousePosition.Y >= completePosition.Y && mousePosition.Y <= completePosition.Y + fontSpecification.Size + explorerLineMargin &&
                Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                EditorService.Root.GetService<Selection>().Set([instance]);
            }
        }

        currentHeight += fontSpecification.Size + explorerLineMargin;
        
        if (IsExpanded(instance))
        {
            using RentedSpan<Instance?> rentedChildren = instance.GetChildren_Fast();

            currentTabulation += 20;

            for (int i = 0; i < rentedChildren.Values.Length; i++)
            {
                Instance? childInstance = rentedChildren.Values[i];
                if (childInstance == null)
                    continue;
                RenderItem(childInstance);
            }

            currentTabulation -= 20;
        }
    }
    public override void Render(EditorWindow window, Vector2 offset, Vector2 windowSize)
    {
        absoluteSize = Size.Resolve(windowSize);
        absoluteOffset = offset;

        currentHeight = 0;
        currentTabulation = 0;

        using RentedSpan<Instance?> rentedDataModelChildren = EditorService.Root.GetChildren_Fast();

        allInstances.Clear();

        for (int i = 0; i < rentedDataModelChildren.Values.Length; i++)
        {
            Instance? childInstance = rentedDataModelChildren.Values[i];
            if (childInstance == null)
                continue;
            RenderItem(childInstance);
        }

        if (!EditorService.Root.GetService<CoreEditorService>().KeyboardFocused)
            HandleKeyNavigation();
        HandleScrolling();
    }

    private void HandleKeyNavigation()
    {
        Selection selectionService = EditorService.Root.GetService<Selection>();

        if (Raylib.IsKeyPressed(KeyboardKey.Down))
        {
            Instance[] selection = selectionService.Get();
            if (selection.Length == 1)
            {
                int index = allInstances.IndexOf(selection[0]);
                if (index != -1 && index != allInstances.Count - 1)
                    selectionService.Set([allInstances[index + 1]]);
            }
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Up))
        {
            Instance[] selection = selectionService.Get();
            if (selection.Length == 1)
            {
                int index = allInstances.IndexOf(selection[0]);
                if (index != -1 && index != 0)
                    selectionService.Set([allInstances[index - 1]]);
            }
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Left))
        {
            Instance[] selection = selectionService.Get();
            if (selection.Length == 1)
            {
                if (selection[0].Parent is not null && selection[0].Parent is not DataModel)
                {
                    Collapse(selection[0].Parent!);
                    selectionService.Set([selection[0].Parent!]);
                }
                else
                    selectionService.Set([selection[0]]);
            }
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Right))
        {
            Instance[] selection = selectionService.Get();
            if (selection.Length == 1)
            {
                if (selection[0].AliveChildrenCount > 0)
                {
                    selectionService.Set([selection[0].GetChildren()[0]]);
                    Expand(selection[0]);
                }
            }
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
}