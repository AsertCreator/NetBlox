using System.Diagnostics;
using System.Numerics;
using NetBlox.Instances;
using NetBlox.Instances.Parts;
using NetBlox.Instances.Services;
using NetBlox.Instances.UI;
using NetBlox.Network;
using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;

namespace NetBlox.Rendering;

public class WorkspaceRendererViewport : RendererViewport
{
    public Camera3D MainCamera;
    public Camera3D LightCamera;

    public Texture2D? SkyboxTop;
    public Texture2D? SkyboxBottom;
    public Texture2D? SkyboxLeft;
    public Texture2D? SkyboxRight;
    public Texture2D? SkyboxFront;
    public Texture2D? SkyboxBack;

    public Shader SpecularLightingInstancedShader;
    public Shader SpecularLightingShader;
    public Shader ShadowMapShader;
    public RenderTexture2D ShadowMap;
    public RenderingEventArgs RenderingEventArgs;

    public int SpeedMultiplier = 1;

    private int uniform_viewPosition;
    private int uniform_lightPosition;
    private int uniform_shadowmap;
    private int uniform_lightVP;

    private int uniform_instanced_viewPosition;
    private int uniform_instanced_lightPosition;
    private int uniform_instanced_shadowmap;
    private int uniform_instanced_lightVP;

    private LRUCache<PartSpecification, GCMesh> partRenderBufferMeshCache;
    private Dictionary<PartSpecification, List<BasePart>> partRenderBuffer = [];
    private int maxPartRenderBufferDepth = 0;
    private GameSchedulerPerfEntry[] lastperfEntry;
    private Mesh cubeMesh;
    private Mesh sphereMesh;

    public unsafe WorkspaceRendererViewport(GameRenderer gameRenderer) : base(gameRenderer)
    {
        MainCamera = new Camera3D()
        {
            Position = new Vector3(40, 20, 0),
            Target = new Vector3(0, 0, 0),
            Up = Vector3.UnitY,
            FovY = 90
        };

        RenderingEventArgs = new RenderingEventArgs(this);

        lastperfEntry = new GameSchedulerPerfEntry[1];

        partRenderBufferMeshCache = new LRUCache<PartSpecification, GCMesh>(4096); // idk if this is a good value

        gameRenderer.GameManager.GameAssetManager.QuickLoad("rbxasset://skybox/bluecloud_up.png")
            ?.AddCallbackForSuccess(x => SkyboxTop = gameRenderer.GameManager.GameAssetManager.LoadTextureFromPath(x.LocalDownloadPath!));
        gameRenderer.GameManager.GameAssetManager.QuickLoad("rbxasset://skybox/bluecloud_dn.png")
            ?.AddCallbackForSuccess(x => SkyboxBottom = gameRenderer.GameManager.GameAssetManager.LoadTextureFromPath(x.LocalDownloadPath!));
        gameRenderer.GameManager.GameAssetManager.QuickLoad("rbxasset://skybox/bluecloud_lf.png")
            ?.AddCallbackForSuccess(x => SkyboxLeft = gameRenderer.GameManager.GameAssetManager.LoadTextureFromPath(x.LocalDownloadPath!));
        gameRenderer.GameManager.GameAssetManager.QuickLoad("rbxasset://skybox/bluecloud_rt.png")
            ?.AddCallbackForSuccess(x => SkyboxRight = gameRenderer.GameManager.GameAssetManager.LoadTextureFromPath(x.LocalDownloadPath!));
        gameRenderer.GameManager.GameAssetManager.QuickLoad("rbxasset://skybox/bluecloud_ft.png")
            ?.AddCallbackForSuccess(x => SkyboxFront = gameRenderer.GameManager.GameAssetManager.LoadTextureFromPath(x.LocalDownloadPath!));
        gameRenderer.GameManager.GameAssetManager.QuickLoad("rbxasset://skybox/bluecloud_bk.png")
            ?.AddCallbackForSuccess(x => SkyboxBack = gameRenderer.GameManager.GameAssetManager.LoadTextureFromPath(x.LocalDownloadPath!));

        gameRenderer.GameManager.GameAssetManager.QuickLoad("rbxasset://shaders/specular")?.AddCallbackForSuccess(x => 
        {
            SpecularLightingShader = gameRenderer.GameManager.GameAssetManager.LoadShaderFromPath(x.LocalDownloadPath!);
            uniform_viewPosition = Raylib.GetShaderLocation(SpecularLightingShader, "viewPosition");
            uniform_lightPosition = Raylib.GetShaderLocation(SpecularLightingShader, "lightPosition");
            uniform_shadowmap = Raylib.GetShaderLocation(SpecularLightingShader, "shadowmap");
            uniform_lightVP = Raylib.GetShaderLocation(SpecularLightingShader, "lightVP");
            SpecularLightingShader.Locs[(int)ShaderLocationIndex.MatrixMvp] = Raylib.GetShaderLocation(SpecularLightingShader, "mvp");
            SpecularLightingShader.Locs[(int)ShaderLocationIndex.ColorDiffuse] = Raylib.GetShaderLocation(SpecularLightingShader, "colDiffuse");
        });
        gameRenderer.GameManager.GameAssetManager.QuickLoad("rbxasset://shaders/specular_instanced")?.AddCallbackForSuccess(x => 
        {
            SpecularLightingInstancedShader = gameRenderer.GameManager.GameAssetManager.LoadShaderFromPath(x.LocalDownloadPath!);
            uniform_instanced_viewPosition = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "viewPosition");
            uniform_instanced_lightPosition = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "lightPosition");
            uniform_instanced_shadowmap = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "shadowmap");
            uniform_instanced_lightVP = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "lightVP");
            SpecularLightingInstancedShader.Locs[(int)ShaderLocationIndex.MatrixMvp] = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "mvp");
            SpecularLightingInstancedShader.Locs[(int)ShaderLocationIndex.VertexColor] = Raylib.GetShaderLocationAttrib(SpecularLightingInstancedShader, "vertexColor");
        });
        gameRenderer.GameManager.GameAssetManager.QuickLoad("rbxasset://shaders/shadowmap")?.AddCallbackForSuccess(x => 
        {
            ShadowMapShader = gameRenderer.GameManager.GameAssetManager.LoadShaderFromPath(x.LocalDownloadPath!);
        });
        
        ShadowMap = CreateShadowmap(1024, 1024);
    }
    public virtual void DrawSky()
    {
        Vector3 position = MainCamera.Position;
        float skyboxsize = 1;

        Quaternion topPartRotation = Raymath.QuaternionFromEuler(0, 90 / (180 / MathF.PI), 0);

        if (SkyboxTop.HasValue)
            RenderUtils.DrawCubeTextureRec(SkyboxTop.Value, position + new Vector3(0, skyboxsize, 0), topPartRotation, 
                skyboxsize, skyboxsize, skyboxsize, Color.White, Structs.Faces.Bottom);
        if (SkyboxLeft.HasValue)
            RenderUtils.DrawCubeTextureRec(SkyboxLeft.Value, position + new Vector3(-skyboxsize, 0, 0), Quaternion.Identity, 
                skyboxsize, skyboxsize, skyboxsize, Color.White, Structs.Faces.Right);
        if (SkyboxRight.HasValue)
            RenderUtils.DrawCubeTextureRec(SkyboxRight.Value, position + new Vector3(skyboxsize, 0, 0), Quaternion.Identity, 
                skyboxsize, skyboxsize, skyboxsize, Color.White, Structs.Faces.Left);
        if (SkyboxBottom.HasValue)
            RenderUtils.DrawCubeTextureRec(SkyboxBottom.Value, position + new Vector3(0, -skyboxsize, 0), topPartRotation, 
                skyboxsize, skyboxsize, skyboxsize, Color.White, Structs.Faces.Top);
        if (SkyboxFront.HasValue)
            RenderUtils.DrawCubeTextureRec(SkyboxFront.Value, position + new Vector3(0, 0, skyboxsize), Quaternion.Identity, 
                skyboxsize, skyboxsize, skyboxsize, Color.White, Structs.Faces.Back);
        if (SkyboxBack.HasValue)
            RenderUtils.DrawCubeTextureRec(SkyboxBack.Value, position + new Vector3(0, 0, -skyboxsize), Quaternion.Identity, 
                skyboxsize, skyboxsize, skyboxsize, Color.White, Structs.Faces.Front);
    }
    public virtual string ConstructDebugString()
    {
        string value = "NetBlox" + 
            ", fps: " + Raylib.GetFPS() + 
            ", actor count: " + (GameRenderer.GameManager.PhysicsSolver != null ? GameRenderer.GameManager.PhysicsSolver.GetActorCount() : 0) + 
            ", instance count: " + GameRenderer.GameManager.GameRegistry.GetInstanceCount();
        
        NetworkClient? client = GameRenderer.Root.FindService<NetworkClient>();

        if (client != null)
            value += ", ping: " + client.LastServerPingValue.TotalMilliseconds + " ms";

        return value;
    }
    public virtual void RenderDebugString()
    {
        Font font = GameRenderer.FontRegistry.LoadFontFromSpecification(GameRenderer.DefaultFontSpecification);
        Raylib.DrawTextEx(font, ConstructDebugString(), new Vector2(0, 0), GameRenderer.DefaultFontSpecification.Size, 0, Color.White);

        Raylib.DrawTextEx(font, GameRenderer.StatusText, new Vector2(50, 50), GameRenderer.DefaultFontSpecification.Size, 0, Color.White);
    }
    public virtual void RenderDebugInfo()
    {
        Font font = GameRenderer.FontRegistry.LoadFontFromSpecification(GameRenderer.DefaultFontSpecification);

        RenderDebugString();

        if (lastperfEntry.Length - 1 < GameRenderer.GameManager.GameScheduler.SchedulerTasks.Count)
            Array.Resize(ref lastperfEntry, GameRenderer.GameManager.GameScheduler.SchedulerTasks.Count + 1);

        int count = GameRenderer.GameManager.GameScheduler.ReadPerformance(lastperfEntry);

        float totalMicroseconds = 0;
        for (int i = 0; i < count; i++)
            totalMicroseconds += lastperfEntry[i].TimeUsed;

        Vector2 vector2 = new Vector2(Raylib.GetScreenWidth() - 150, 150);
        
        float ms = 0;
        for (int i = 0; i < count; i++)
        {
            BrickColor brickColor = BrickColor.MediumStoneGrey;
            if (lastperfEntry[i].Task != null)
                brickColor = BrickColor.AllBrickColors[lastperfEntry[i].Task!.GetHashCode() % BrickColor.AllBrickColors.Length];
            
            string name = "Idle";
            if (lastperfEntry[i].Task != null)
                name = lastperfEntry[i].Task!.DebugName;

            Raylib.DrawCircleSector(vector2, 130, 
                ms / (float)totalMicroseconds * 360,
                (ms + lastperfEntry[i].TimeUsed) / (float)totalMicroseconds * 360, 18, brickColor.Color3);

            ms += lastperfEntry[i].TimeUsed;

            Raylib.DrawTextEx(font, name + " " + (lastperfEntry[i].TimeUsed / totalMicroseconds * 100) + "%", 
                new Vector2(vector2.X - 130, vector2.Y + 140 + i * 18), 16, 0, brickColor.Color3);
        }
    }
    public override void RenderFrame()
    {
        Raylib.ClearBackground(Color.SkyBlue);

        if (GameRenderer.GameManager.RootModel.GetService<Lighting>().RealTimeShadows)
        {
            LightCamera = new Camera3D()
            {
                Projection = CameraProjection.Orthographic,
                Position = GameRenderer.GameManager.RootModel.GetService<Lighting>().SunPosition * 2 + MainCamera.Position,
                Target = MainCamera.Position,
                Up = Vector3.UnitY
            };

            Raylib.BeginTextureMode(ShadowMap);
            Raylib.BeginMode3D(LightCamera);
            Raylib.ClearBackground(Color.Black);

            RenderingEventArgs.RenderingForShadowMap = true;
            GameRenderer.GameManager.TryGetEventForId(GameEvent.EVENT_RENDER3D)?.Fire(RenderingEventArgs);
            
            Matrix4x4 matlightVP = Raymath.MatrixMultiply(Rlgl.GetMatrixModelview(), Rlgl.GetMatrixProjection());

            Raylib.EndMode3D();
            Raylib.EndTextureMode();

            Raylib.SetShaderValueTexture(SpecularLightingShader, uniform_shadowmap, ShadowMap.Depth);
            Raylib.SetShaderValueTexture(SpecularLightingInstancedShader, uniform_instanced_shadowmap, ShadowMap.Depth);

            Raylib.SetShaderValueMatrix(SpecularLightingShader, uniform_lightVP, matlightVP);
            Raylib.SetShaderValueMatrix(SpecularLightingInstancedShader, uniform_instanced_lightVP, matlightVP);
        }

        Raylib.SetShaderValue(SpecularLightingShader, uniform_viewPosition, MainCamera.Position, ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(SpecularLightingShader, uniform_lightPosition, 
            GameRenderer.GameManager.RootModel.GetService<Lighting>().SunPosition, ShaderUniformDataType.Vec3);

        Raylib.SetShaderValue(SpecularLightingInstancedShader, uniform_instanced_viewPosition, MainCamera.Position, ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(SpecularLightingInstancedShader, uniform_instanced_lightPosition, 
            GameRenderer.GameManager.RootModel.GetService<Lighting>().SunPosition, ShaderUniformDataType.Vec3);

        Raylib.BeginMode3D(MainCamera);
        Rlgl.DisableDepthTest();

        DrawSky();

        Rlgl.DrawRenderBatchActive();
        Rlgl.EnableDepthTest();

        if (Raylib.IsMouseButtonPressed(MouseButton.Right))
            Raylib.DisableCursor();

        if (Raylib.IsMouseButtonDown(MouseButton.Right))
        {
            Vector3 position = MainCamera.Position;
            Vector3 target = MainCamera.Target;

            // an already slow update loop, slowified even more.
            for (int i = 0; i < SpeedMultiplier; i++)
            {
                Raylib.UpdateCamera(ref MainCamera, CameraMode.FirstPerson);
                Raylib.UpdateCamera(ref MainCamera, CameraMode.FirstPerson);
            }

            if (MainCamera.Position != position || MainCamera.Target != target)
            {
                NetworkClient? networkClient = GameRenderer.Root.FindService<NetworkClient>();
                if (networkClient != null && networkClient.IsConnected)
                {
                    NetworkPacket packet = NPClientCameraUpdate.Create(MainCamera.Position, MainCamera.Target);
                    networkClient.CurrentServerConnection?.SendPacketUnreliable(packet);
                }
            }
        }

        if (Raylib.IsKeyPressed(KeyboardKey.L))
        {
            GameRenderer.GameManager.PhysicsSolver!.CanRun = !GameRenderer.GameManager.PhysicsSolver!.CanRun;
        }
        if (Raylib.IsKeyPressed(KeyboardKey.K))
        {
            Part part = GameRenderer.GameManager.GameRegistry.Construct<Part>();
            part.Size = new Vector3(2, 2, Random.Shared.Next(2, 10));
            part.BrickColor = BrickColor.Random();
            part.Anchored = false;
            part.Position = MainCamera.Position;

            int surfaceLottery = Random.Shared.Next(4);
            if (surfaceLottery == 0)
                part.TopSurface = SurfaceType.Studs;
            else if (surfaceLottery == 1)
                part.TopSurface = SurfaceType.Inlet;
            else if (surfaceLottery == 2)
                part.TopSurface = SurfaceType.Universal;
            else if (surfaceLottery == 3)
                part.TopSurface = SurfaceType.Glue;

            part.Parent = GameRenderer.GameManager.RootModel.GetService<Workspace>();
        }
        if (Raylib.IsKeyPressed(KeyboardKey.O))
        {
            for (int i = 0; i < 10; i++)
            {
                Part part = GameRenderer.GameManager.GameRegistry.Construct<Part>();
                part.Size = new Vector3(2, 2, 4);
                part.BrickColor = BrickColor.Blue;
                part.Anchored = false;
                part.Position = MainCamera.Position + new Vector3(0, 2.5f * i, 0);
                if (Raylib.IsKeyDown(KeyboardKey.LeftAlt))
                    part.Shape = PartType.Ball;

                part.Parent = GameRenderer.GameManager.RootModel.GetService<Workspace>();
            }
        }
        if (Raylib.IsKeyPressed(KeyboardKey.C))
        {
            for (int i = 0; i < 100; i++)
            {
                Part part = GameRenderer.GameManager.GameRegistry.Construct<Part>();
                part.Size = new Vector3(2, 2, 4);
                part.BrickColor = BrickColor.Random();
                part.Anchored = false;
                part.Position = MainCamera.Position + new Vector3(0, 2.5f * i, 0);
                if (Raylib.IsKeyDown(KeyboardKey.LeftAlt))
                    part.Shape = PartType.Ball;

                part.Parent = GameRenderer.GameManager.RootModel.GetService<Workspace>();
            }
        }
        if (Raylib.IsKeyPressed(KeyboardKey.V))
        {
            for (int i = 0; i < 300; i++)
            {
                Part part = GameRenderer.GameManager.GameRegistry.Construct<Part>();
                part.Size = new Vector3(2, 2, 4);
                part.BrickColor = BrickColor.Random();
                part.Anchored = false;
                part.Position = MainCamera.Position + new Vector3(Random.Shared.Next(-70, 70), Random.Shared.Next(0, 150), Random.Shared.Next(-70, 70));
                if (Raylib.IsKeyDown(KeyboardKey.LeftAlt))
                    part.Shape = PartType.Ball;

                part.Parent = GameRenderer.GameManager.RootModel.GetService<Workspace>();
            }
        }
        if (Raylib.IsKeyPressed(KeyboardKey.I))
        {
            Part part = GameRenderer.GameManager.GameRegistry.Construct<Part>();
            part.Size = new Vector3(2, 2, Random.Shared.Next(2, 10));
            part.Shape = PartType.Ball;
            part.BrickColor = BrickColor.Random();
            part.Anchored = false;
            part.Position = MainCamera.Position;
            part.Parent = GameRenderer.GameManager.RootModel.GetService<Workspace>();
        }
        if (Raylib.IsKeyPressed(KeyboardKey.M))
        {
            Hint hint = GameRenderer.GameManager.GameRegistry.Construct<Hint>();
            hint.Parent = GameRenderer.GameManager.RootModel.GetService<Workspace>();
            hint.Text = "This is a Hint class example";

            Message message = GameRenderer.GameManager.GameRegistry.Construct<Message>();
            message.Parent = GameRenderer.GameManager.RootModel.GetService<Workspace>();
            message.Text = "This is a Message class example";

            GameRenderer.GameManager.RootModel.GetService<Debris>().AddItem(hint, 2);
            GameRenderer.GameManager.RootModel.GetService<Debris>().AddItem(message, 3);
        }
        if (Raylib.IsKeyPressed(KeyboardKey.G))
        {
            ScreenGui screenGui = GameRenderer.GameManager.GameRegistry.Construct<ScreenGui>();
            screenGui.Parent = GameRenderer.GameManager.RootModel.GetService<StarterGui>();

            for (float i = 0; i <= 1; i += 0.5f)
            {
                for (float j = 0; j <= 1; j += 0.5f)
                {
                    TextLabel textLabel = GameRenderer.GameManager.GameRegistry.Construct<TextLabel>();
                    textLabel.Parent = screenGui;
                    textLabel.Text = "Grettings, my new realm!";
                    textLabel.Size = new UDim2(0, 300, 0, 150);
                    textLabel.Position = new UDim2(0.5f, 0, 0.5f, 0);
                    textLabel.AnchorPoint = new Vector2(i, j);
                    textLabel.BackgroundColor = BrickColor.Random();
                    textLabel.TextColor = BrickColor.Random();
                    textLabel.BackgroundTransparency = 0.33f;
                }
            }

            TextButton shutdownButton = GameRenderer.GameManager.GameRegistry.Construct<TextButton>();
            shutdownButton.Parent = screenGui;
            shutdownButton.Text = "Shutdown";
            shutdownButton.Size = new UDim2(0, 200, 0, 25);
            shutdownButton.Position = new UDim2(0.5f, 0, 0.8f, 0);
            shutdownButton.AnchorPoint = new Vector2(0.5f, 0.5f);
            shutdownButton.SetVerb("shutdown");

            TextButton doNothingButton = GameRenderer.GameManager.GameRegistry.Construct<TextButton>();
            doNothingButton.Parent = screenGui;
            doNothingButton.Text = "Do nothing";
            doNothingButton.Size = new UDim2(0, 200, 0, 25);
            doNothingButton.Position = new UDim2(0.5f, 0, 0.8f, 27);
            doNothingButton.AnchorPoint = new Vector2(0.5f, 0.5f);

            GameRenderer.GameManager.RootModel.GetService<StarterGui>().ShowDevelopmentGui = true;
            GameRenderer.GameManager.RootModel.GetService<Debris>().AddItem(screenGui, 5);
        }

        if (Raylib.IsKeyPressed(KeyboardKey.One))
        {
            SpeedMultiplier = 1;
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Two))
        {
            SpeedMultiplier = 2;
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Three))
        {
            SpeedMultiplier = 3;
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Four))
        {
            SpeedMultiplier = 4;
        }
        

        for (int i = 0; i < SpeedMultiplier; i++)
        {
            if (Raylib.IsKeyDown(KeyboardKey.LeftShift))
            {
                MainCamera.Position.Y -= 0.2f;
                MainCamera.Target.Y -= 0.2f;
            }
            if (Raylib.IsKeyDown(KeyboardKey.Space))
            {
                MainCamera.Position.Y += 0.2f;
                MainCamera.Target.Y += 0.2f;
            }
        }

        if (Raylib.IsMouseButtonReleased(MouseButton.Right))
            Raylib.EnableCursor();

        RenderingEventArgs.RenderingForShadowMap = false;
        GameRenderer.GameManager.TryGetEventForId(GameEvent.EVENT_RENDER3D)?.Fire(RenderingEventArgs);

        DrawPartRenderBuffer();

        if (GameRenderer.DebugFlag)
            DrawGizmos(true);

        Raylib.EndMode3D();

        if (GameRenderer.DebugFlag)
            DrawGizmos(false);

        GameRenderer.GameManager.TryGetEventForId(GameEvent.EVENT_RENDERGUI_LEVEL0)?.Fire();
        GameRenderer.GameManager.TryGetEventForId(GameEvent.EVENT_RENDERGUI_LEVEL1)?.Fire();
        GameRenderer.GameManager.TryGetEventForId(GameEvent.EVENT_RENDERGUI_LEVEL2)?.Fire();
        GameRenderer.GameManager.TryGetEventForId(GameEvent.EVENT_RENDERGUI_LEVEL3)?.Fire();

        if (GameRenderer.DebugFlag)
            RenderDebugInfo();

        // Raylib.DrawTexture(ShadowMap.Texture, 0, 0, Color.White);
    }
    public virtual void DrawPartRenderBuffer()
    {
        if (sphereMesh.VaoId == 0)
            sphereMesh = Raylib.GenMeshSphere(0.5f, 9, 15);
        if (cubeMesh.VaoId == 0)
            cubeMesh = Raylib.GenMeshCube(1, 1, 1);

        Span<PartRenderInstanceInfo> matrixies = stackalloc PartRenderInstanceInfo[maxPartRenderBufferDepth];

        foreach (KeyValuePair<PartSpecification, List<BasePart>> kvp in partRenderBuffer)
        {
            PartSpecification partSpecification = kvp.Key;

            if (partSpecification.Shape == PartType.Block)
            {
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    BasePart basePart = kvp.Value[i];
                    Matrix4x4 matrix = Matrix4x4.CreateScale(basePart.Size.X, basePart.Size.Y, basePart.Size.Z);
                    matrix *= Matrix4x4.CreateFromQuaternion(basePart.QuaternionRotation);
                    matrix *= Matrix4x4.CreateTranslation(basePart.Position.X, basePart.Position.Y, basePart.Position.Z);
                    matrix = Matrix4x4.Transpose(matrix);
                    matrixies[i].Transform = Raymath.MatrixToFloatV(matrix);
                    matrixies[i].Color.X = basePart.Color3.R / 255f;
                    matrixies[i].Color.Y = basePart.Color3.G / 255f;
                    matrixies[i].Color.Z = basePart.Color3.B / 255f;
                    matrixies[i].Color.W = 1;
                }

                RenderUtils.CustomDrawMeshInstanced(cubeMesh, matrixies[..kvp.Value.Count], this);
            }
            else if (partSpecification.Shape == PartType.Ball)
            {
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    BasePart basePart = kvp.Value[i];
                    Vector3 size = basePart.Size;
                    float maxsize = size.X;

                    if (size.Y > maxsize)
                        maxsize = size.Y;
                    if (size.Z > maxsize)
                        maxsize = size.Z;

                    maxsize /= 2;

                    Matrix4x4 matrix = Matrix4x4.CreateScale(maxsize, maxsize, maxsize);
                    matrix *= Matrix4x4.CreateFromQuaternion(basePart.QuaternionRotation);
                    matrix *= Matrix4x4.CreateTranslation(basePart.Position.X, basePart.Position.Y, basePart.Position.Z);
                    matrix = Matrix4x4.Transpose(matrix);
                    matrixies[i].Transform = Raymath.MatrixToFloatV(matrix);
                    matrixies[i].Color.X = basePart.Color3.R / 255f;
                    matrixies[i].Color.Y = basePart.Color3.G / 255f;
                    matrixies[i].Color.Z = basePart.Color3.B / 255f;
                    matrixies[i].Color.W = 1;
                }

                RenderUtils.CustomDrawMeshInstanced(sphereMesh, matrixies[..kvp.Value.Count], this);
            }
        }

        partRenderBuffer.Clear();
        maxPartRenderBufferDepth = 0;
    }
    public virtual void DrawGizmos(bool in3d)
    {
        NetworkServer? networkServer = GameRenderer.Root.FindService<NetworkServer>();

        if (networkServer != null)
        {
            Font font = GameRenderer.FontRegistry.LoadFontFromSpecification(GameRenderer.DefaultFontSpecification);
            Players players = GameRenderer.Root.GetService<Players>();
            using RentedSpan<Instance?> rented = players.GetChildren_Fast();

            if (in3d)
                Raylib.BeginShaderMode(SpecularLightingShader);

            for (int i = 0; i < rented.Values.Length; i++)
            {
                Player? player = rented.Values[i] as Player;
                if (player == null)
                    continue;

                if (in3d)
                {
                    Color color = player.GetPlayerColor().Color3;
                    color.A = 128;
                    
                    Raylib.DrawSphere(player.CurrentCameraPosition, 1, color);
                }
                else
                {
                    Vector3 textpos = player.CurrentCameraPosition + new Vector3(0, 1.75f, 0);
                    Vector2 vector2 = Raylib.GetWorldToScreen(textpos, MainCamera);
                    Vector2 size = Raylib.MeasureTextEx(font, player.Name,  GameRenderer.DefaultFontSpecification.Size, 0);

                    Vector3 direction = MainCamera.Target - MainCamera.Position;
                    Vector3 textdirection = textpos - MainCamera.Position;

                    if (Vector3.Dot(direction, textdirection) > 0)
                        Raylib.DrawTextEx(font, player.Name, vector2 - size / 2, GameRenderer.DefaultFontSpecification.Size, 0, Color.White);
                }
            }

            if (in3d)
                Raylib.EndShaderMode();
        }
    }
    public void WritePart(BasePart part)
    {
        PartSpecification partSpecification = default;

        if (part is Part concretePart)
        {
            partSpecification.Shape = concretePart.Shape;
            partSpecification.TopSurface = concretePart.TopSurface;
            partSpecification.LeftSurface = concretePart.LeftSurface;
            partSpecification.RightSurface = concretePart.RightSurface;
            partSpecification.BottomSurface = concretePart.BottomSurface;
            partSpecification.FrontSurface = concretePart.FrontSurface;
            partSpecification.BackSurface = concretePart.BackSurface;
        }
        else
        {
            throw new NotImplementedException("Unsupported part type");
        }

        if (!partRenderBuffer.TryGetValue(partSpecification, out _))
            partRenderBuffer[partSpecification] = new();
        
        partRenderBuffer[partSpecification].Add(part);
        if (partRenderBuffer[partSpecification].Count > maxPartRenderBufferDepth)
            maxPartRenderBufferDepth = partRenderBuffer[partSpecification].Count;
    }
    private unsafe RenderTexture2D CreateShadowmap(int width, int height) 
    {
        // standard fbo
        RenderTexture2D target = default;
        target.Id = Rlgl.LoadFramebuffer();

        if(target.Id > 0)
        {
            Rlgl.EnableFramebuffer(target.Id);

            // colour component, this is basically unused but i couldnt be bothered working a fragment shader without it.
            // let opengl do the depth calculation internally this way
            target.Texture.Id = Rlgl.LoadTexture((void*)0, width, height, PixelFormat.UncompressedR8G8B8A8, 1);
            target.Texture.Width = width;
            target.Texture.Height = height;
            target.Texture.Format = PixelFormat.UncompressedR8G8B8A8; // encode colour as 4 8 bit floats
            target.Texture.Mipmaps = 1;

            // disable renderbuffer, use a texture instead for depth
            target.Depth.Id = Rlgl.LoadTextureDepth(width, height, 0);
            target.Depth.Width = width;
            target.Depth.Height = height;
            target.Depth.Format = PixelFormat.UncompressedR32; // encode depth as a single 32bit float
            target.Depth.Mipmaps = 1;

            // bind textures to framebuffer, note RL_ATTACHMENT_DEPTH for depth component
            Rlgl.FramebufferAttach(target.Id, target.Texture.Id, FramebufferAttachType.ColorChannel0, FramebufferAttachTextureType.Texture2D, 0);
            Rlgl.FramebufferAttach(target.Id, target.Depth.Id, FramebufferAttachType.Depth, FramebufferAttachTextureType.Texture2D, 0);

            Rlgl.DisableFramebuffer();
        }
        else
        {
            Trace.TraceError("Shadowmap creation failed!");
        }

        return target;
    }
}