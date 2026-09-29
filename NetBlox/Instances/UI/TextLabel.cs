using System.Diagnostics;
using System.Numerics;
using System.Text;
using NetBlox.Rendering;
using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;
using Rectangle = NetBlox.Structs.Rectangle;

namespace NetBlox.Instances.UI;

[Creatable]
public class TextLabel : GuiLabel
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public string Text { get; set; } = "";
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public BrickColor TextColor
    { 
        get => BrickColor.GetBrickColorByClosestColor3(TextColor3);
        set => TextColor3 = value.Color3;
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public Color3 TextColor3 { get; set; }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public float TextSize { get; set; } = 16;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public Color3 TextStrokeColor3 { get; set; }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public float TextStrokeTransparency { get; set; } = 0;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public float TextTransparency { get; set; } = 1;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public bool TextWrapped { get; set; } = false;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public TextXAlignment TextXAlignment { get; set; } = TextXAlignment.Center;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public TextYAlignment TextYAlignment { get; set; } = TextYAlignment.Center;

    private FontSpecification lastFontSpecification;
    private Vector2 lastAbsoluteSize;
    private string? lastActualText;
    private string? lastEffectiveText;

    public override string ClassName => nameof(TextLabel);

    public TextLabel(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public FontSpecification GetFontSpecification()
    {
        FontSpecification fontSpecification = GameManager.GameRenderer!.DefaultFontSpecification;
        fontSpecification.Size = TextSize;
        return fontSpecification;
    }

    public override void RenderUI(Rectangle containerRectangle, Stack<Rectangle> cdclipStack, Rectangle currentClip)
    {
        if (Visible)
        {
            Rectangle rectangle = ResolveBoundingRectangle(containerRectangle);
            FontSpecification fontSpecification = GetFontSpecification();
            Font font = GameManager.GameRenderer!.FontRegistry.LoadFontFromSpecification(fontSpecification);

            Color color = BackgroundColor3;
            color.A = (byte)(255 * (1 - BackgroundTransparency));
            Raylib.DrawRectangle((int)rectangle.X, (int)rectangle.Y, (int)rectangle.Width, (int)rectangle.Height, color);
            
            if (BorderSizePixel > 0)
                Raylib.DrawRectangleLines((int)rectangle.X, (int)rectangle.Y, (int)rectangle.Width, (int)rectangle.Height, BorderColor3);

            Vector2 textMeasuredSize = Raylib.MeasureTextEx(font, Text, fontSpecification.Size, 0);
            Vector2 textPosition = default;

            switch (TextXAlignment)
            {
            case TextXAlignment.Left:
                textPosition.X = 0;
                break;
            case TextXAlignment.Center:
                textPosition.X = rectangle.Size.X / 2 - textMeasuredSize.X / 2;
                break;
            case TextXAlignment.Right:
                textPosition.X = rectangle.Size.X - textMeasuredSize.X;
                break;    
            }

            switch (TextYAlignment)
            {
            case TextYAlignment.Top:
                textPosition.Y = 0;
                break;
            case TextYAlignment.Center:
                textPosition.Y = rectangle.Size.Y / 2 - textMeasuredSize.Y / 2;
                break;
            case TextYAlignment.Bottom:
                textPosition.Y = rectangle.Size.Y - textMeasuredSize.Y;
                break;    
            }

            Color textColor = TextColor3;
            textColor.A = (byte)(255 * TextTransparency);

            if (TextWrapped)
                UpdateEffectiveText(fontSpecification);
            else
            {
                lastActualText = Text;
                lastEffectiveText = Text;
            }

            if (TextStrokeTransparency > 0)
            {
                Color textStrokeColor = TextStrokeColor3;
                textStrokeColor.A = (byte)(255 * TextStrokeTransparency);
                Vector2 v2 = rectangle.Position + textPosition;

                Raylib.DrawTextEx(font, lastEffectiveText, v2 + new Vector2(1, 1), fontSpecification.Size, 0, textStrokeColor);
                Raylib.DrawTextEx(font, lastEffectiveText, v2 + new Vector2(1, 0), fontSpecification.Size, 0, textStrokeColor);
                Raylib.DrawTextEx(font, lastEffectiveText, v2 + new Vector2(1, -1), fontSpecification.Size, 0, textStrokeColor);
                Raylib.DrawTextEx(font, lastEffectiveText, v2 + new Vector2(0, 1), fontSpecification.Size, 0, textStrokeColor);
                Raylib.DrawTextEx(font, lastEffectiveText, v2 + new Vector2(0, -1), fontSpecification.Size, 0, textStrokeColor);
                Raylib.DrawTextEx(font, lastEffectiveText, v2 + new Vector2(-1, 1), fontSpecification.Size, 0, textStrokeColor);
                Raylib.DrawTextEx(font, lastEffectiveText, v2 + new Vector2(-1, 0), fontSpecification.Size, 0, textStrokeColor);
                Raylib.DrawTextEx(font, lastEffectiveText, v2 + new Vector2(-1, -1), fontSpecification.Size, 0, textStrokeColor);
            }

            Raylib.DrawTextEx(font, lastEffectiveText, rectangle.Position + textPosition, fontSpecification.Size, 0, textColor);

            base.RenderUI(containerRectangle, cdclipStack, currentClip);
        }
    }
    public void ForceUpdateEffectiveText(FontSpecification current)
    {
        // as fallback
        lastActualText = Text;
        lastEffectiveText = lastActualText;
        lastFontSpecification = current;
        lastAbsoluteSize = AbsoluteSize;

        try
        {
            string[] words = Text.Split(' ');
            float x = 0;

            Font font = GameManager.GameRenderer!.FontRegistry.LoadFontFromSpecification(current);
            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                if (i > 0)
                    word = ' ' + word;

                Vector2 vector2 = Raylib.MeasureTextEx(font, word, current.Size, 0);
                if (x + vector2.X > AbsoluteSize.X)
                {
                    word = "\n" + word.Substring(1);
                    x = 0;
                }
                x += vector2.X;

                stringBuilder.Append(word);
            }

            lastEffectiveText = stringBuilder.ToString();
        }
        catch
        {
            Trace.TraceWarning("Text wrapping failured");
            return;
        }
    }
    public void UpdateEffectiveText(FontSpecification current)
    {
        if (current.GetHashCode() != lastFontSpecification.GetHashCode() ||
            lastActualText != Text ||
            lastAbsoluteSize != AbsoluteSize)
        {
            ForceUpdateEffectiveText(current);
            return;
        }

        lastFontSpecification = current;
        lastActualText = Text;
        lastAbsoluteSize = AbsoluteSize;
    }
    public override bool IsA(string className)
    {
        if (className != nameof(TextLabel))
            return base.IsA(className);
        return true;
    }
}