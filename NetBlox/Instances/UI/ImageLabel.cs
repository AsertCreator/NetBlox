using System.Diagnostics;
using System.Numerics;
using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;
using Rectangle = NetBlox.Structs.Rectangle;

namespace NetBlox.Instances.UI;

[Creatable]
public class ImageLabel : GuiLabel
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public string Image { get; set; } = "";
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public Color3 ImageColor3 { get; set; } = new Color3(255, 255, 255);

    private string lastImage = "";
    private Texture2D image;

    public override string ClassName => nameof(ImageLabel);

    public ImageLabel(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override void RenderUI(Rectangle containerRectangle, Stack<Rectangle> cdclipStack, Rectangle currentClip)
    {
        if (Visible)
        {
            Rectangle rectangle = ResolveBoundingRectangle(containerRectangle);

            Color color = BackgroundColor3;
            color.A = (byte)(255 * (1 - BackgroundTransparency));
            Raylib.DrawRectangle((int)rectangle.X, (int)rectangle.Y, (int)rectangle.Width, (int)rectangle.Height, color);
            
            if (BorderSizePixel > 0)
                Raylib.DrawRectangleLines((int)rectangle.X, (int)rectangle.Y, (int)rectangle.Width, (int)rectangle.Height, BorderColor3);

            if (Image != lastImage)
            {
                AssetDownloadTask? task = GameManager.GameAssetManager.QuickLoad(Image);
                if (task == null)
                {
                    Trace.TraceError("Obscure asset loading failure");
                }
                else
                {
                    task.AddCallbackForFailure(x =>
                    {
                        Trace.TraceError("Asset loading failure: " + x.Error);
                        return; 
                    });
                    task.AddCallbackForSuccess(x =>
                    {
                        image = GameManager.GameAssetManager.LoadTextureFromPath(x.LocalDownloadPath!);
                        return;
                    });
                }
            }

            if (image.Id > 0)
            {
                Vector2 size = new Vector2(image.Width, image.Height);
                Vector2 centerposition = rectangle.Position + rectangle.Size / 2;
                float scale = MathF.Min(rectangle.Size.X / size.X, rectangle.Size.Y / size.Y);

                Vector2 newsize = size * scale;

                Raylib.DrawTextureEx(image, centerposition - newsize / 2, 0, scale, Color.White);
            }

            base.RenderUI(containerRectangle, cdclipStack, currentClip);
        }
    }
    public override bool IsA(string className)
    {
        if (className != nameof(ImageLabel))
            return base.IsA(className);
        return true;
    }
}