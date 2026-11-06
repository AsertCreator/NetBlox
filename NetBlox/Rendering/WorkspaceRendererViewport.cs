using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using MoonSharp.Interpreter;
using NetBlox.Instances;
using NetBlox.Instances.Parts;
using NetBlox.Instances.Services;
using NetBlox.Instances.Services.Internal;
using NetBlox.Instances.UI;
using NetBlox.Network;
using NetBlox.Runtime;
using NetBlox.Runtime.Bridges;
using NetBlox.Structs;
using Raylib_cs;
using Model = NetBlox.Instances.Parts.Model;

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

    public Texture2D ParticleFireTexture;
    public Texture2D ParticleSmokeTexture;
    public Texture2D ParticleForceFieldTexture;

    public Shader SpecularLightingInstancedShader;
    public Shader SpecularLightingShader;
    public Shader ShadowMapShader;
    public RenderTexture2D ShadowMap;
    public RenderingEventArgs RenderingEventArgs;

    public List<ParticleStream> ParticleStreams = [];

    public int SpeedMultiplier = 1;
    public bool DisableGizmos = false;

    private int uniform_viewPosition;
    private int uniform_lightPosition;
    private int uniform_shadowmap;
    private int uniform_lightVP;

    private int uniform_instanced_viewPosition;
    private int uniform_instanced_lightPosition;
    private int uniform_instanced_shadowmap;
    private int uniform_instanced_lightVP;
    private int uniform_instanced_surfaceTop;
    private int uniform_instanced_surfaceBottom;
    private int uniform_instanced_surfaceLeft;
    private int uniform_instanced_surfaceRight;
    private int uniform_instanced_surfaceFront;
    private int uniform_instanced_surfaceBack;
    private int uniform_instanced_useSurfaces;
    private int uniform_instanced_blankTexture;
    private int uniform_instanced_studTexture;
    private int uniform_instanced_inletTexture;
    private int uniform_instanced_universalTexture;
    private int uniform_instanced_glueTexture;
    private int uniform_instanced_studNormalTexture;
    private int uniform_instanced_inletNormalTexture;
    private int uniform_instanced_universalNormalTexture;
    private int uniform_instanced_glueNormalTexture;

    private int uniform_instanced_decalTop;
    private int uniform_instanced_decalLeft;
    private int uniform_instanced_decalRight;
    private int uniform_instanced_decalBottom;
    private int uniform_instanced_decalFront;
    private int uniform_instanced_decalBack;

    private int uniform_instanced_decalTopTint;
    private int uniform_instanced_decalLeftTint;
    private int uniform_instanced_decalRightTint;
    private int uniform_instanced_decalBottomTint;
    private int uniform_instanced_decalFrontTint;
    private int uniform_instanced_decalBackTint;

    private Texture2D blankSurfaceTexture;
    private Texture2D studSurfaceTexture;
    private Texture2D inletSurfaceTexture;
    private Texture2D universalSurfaceTexture;
    private Texture2D glueSurfaceTexture;
    private Texture2D studSurfaceNormalTexture;
    private Texture2D inletSurfaceNormalTexture;
    private Texture2D universalSurfaceNormalTexture;
    private Texture2D glueSurfaceNormalTexture;

    private LRUCache<PartSpecification, GCMesh> partRenderBufferMeshCache;
    private Dictionary<PartSpecification, List<BasePart>> opaquePartRenderBuffer = [];
    private Dictionary<PartSpecification, List<BasePart>> transparentPartRenderBuffer = [];
    private int maxPartRenderBufferDepth = 0;
    private GameSchedulerPerfEntry[] lastperfEntry;
    private PartRenderInstanceInfo[]? renderInstances;
    private Mesh cubeMesh;
    private Mesh sphereMesh;
    private Humanoid? followHumanoid;
    private Frustum lastFrustum;

    public unsafe WorkspaceRendererViewport(GameRenderer gameRenderer) : base(gameRenderer)
    {
        MainCamera = new Camera3D()
        {
            Position = new Vector3(10, 5, 0),
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

        gameRenderer.GameManager.GameAssetManager.QuickLoad("rbxasset://textures/particleFire.png")
            ?.AddCallbackForSuccess(x => ParticleFireTexture = gameRenderer.GameManager.GameAssetManager.LoadTextureFromPath(x.LocalDownloadPath!));
        gameRenderer.GameManager.GameAssetManager.QuickLoad("rbxasset://textures/particleSmoke.png")
            ?.AddCallbackForSuccess(x => ParticleSmokeTexture = gameRenderer.GameManager.GameAssetManager.LoadTextureFromPath(x.LocalDownloadPath!));
        gameRenderer.GameManager.GameAssetManager.QuickLoad("rbxasset://textures/particleForceField.png")
            ?.AddCallbackForSuccess(x => ParticleForceFieldTexture = gameRenderer.GameManager.GameAssetManager.LoadTextureFromPath(x.LocalDownloadPath!));

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

            uniform_instanced_surfaceTop = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "surfaceTop");
            uniform_instanced_surfaceBottom = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "surfaceBottom");
            uniform_instanced_surfaceLeft = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "surfaceLeft");
            uniform_instanced_surfaceRight = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "surfaceRight");
            uniform_instanced_surfaceFront = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "surfaceFront");
            uniform_instanced_surfaceBack = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "surfaceBack");
            uniform_instanced_useSurfaces = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "useSurfaces");

            uniform_instanced_blankTexture = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "blankTexture");

            uniform_instanced_studTexture = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "studTexture");
            uniform_instanced_inletTexture = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "inletTexture");
            uniform_instanced_universalTexture = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "universalTexture");

            uniform_instanced_glueTexture = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "glueTexture");
            uniform_instanced_studNormalTexture = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "studNormalTexture");
            uniform_instanced_inletNormalTexture = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "inletNormalTexture");
            uniform_instanced_universalNormalTexture = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "universalNormalTexture");
            uniform_instanced_glueNormalTexture = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "glueNormalTexture");

            uniform_instanced_decalTop = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "decalTop");
            uniform_instanced_decalLeft = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "decalLeft");
            uniform_instanced_decalRight = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "decalRight");
            uniform_instanced_decalBottom = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "decalBottom");
            uniform_instanced_decalFront = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "decalFront");
            uniform_instanced_decalBack = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "decalBack");

            uniform_instanced_decalTopTint = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "decalTopTint");
            uniform_instanced_decalLeftTint = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "decalLeftTint");
            uniform_instanced_decalRightTint = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "decalRightTint");
            uniform_instanced_decalBottomTint = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "decalBottomTint");
            uniform_instanced_decalFrontTint = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "decalFrontTint");
            uniform_instanced_decalBackTint = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "decalBackTint");

            SpecularLightingInstancedShader.Locs[(int)ShaderLocationIndex.MatrixMvp] = Raylib.GetShaderLocation(SpecularLightingInstancedShader, "mvp");
            SpecularLightingInstancedShader.Locs[(int)ShaderLocationIndex.VertexColor] = Raylib.GetShaderLocationAttrib(SpecularLightingInstancedShader, "vertexColor");
        });
        gameRenderer.GameManager.GameAssetManager.QuickLoad("rbxasset://shaders/shadowmap_instanced")?.AddCallbackForSuccess(x =>
        {
            ShadowMapShader = gameRenderer.GameManager.GameAssetManager.LoadShaderFromPath(x.LocalDownloadPath!);
            ShadowMapShader.Locs[(int)ShaderLocationIndex.MatrixMvp] = Raylib.GetShaderLocation(ShadowMapShader, "mvp");
            ShadowMapShader.Locs[(int)ShaderLocationIndex.VertexPosition] = Raylib.GetShaderLocationAttrib(ShadowMapShader, "vertexPosition");
            ShadowMapShader.Locs[(int)ShaderLocationIndex.VertexInstanceTransform] = Raylib.GetShaderLocationAttrib(ShadowMapShader, "instanceTransform");
        });

        gameRenderer.GameManager.GameAssetManager.QuickLoad("rbxasset://textures/blank.png")
            ?.AddCallbackForSuccess(x => blankSurfaceTexture = gameRenderer.GameManager.GameAssetManager.LoadTextureFromPath(x.LocalDownloadPath!));

        gameRenderer.GameManager.GameAssetManager.QuickLoad("rbxasset://textures/studx2.png")
            ?.AddCallbackForSuccess(x => studSurfaceTexture = gameRenderer.GameManager.GameAssetManager.LoadTextureFromPath(x.LocalDownloadPath!));
        gameRenderer.GameManager.GameAssetManager.QuickLoad("rbxasset://textures/inletx2.png")
            ?.AddCallbackForSuccess(x => inletSurfaceTexture = gameRenderer.GameManager.GameAssetManager.LoadTextureFromPath(x.LocalDownloadPath!));
        gameRenderer.GameManager.GameAssetManager.QuickLoad("rbxasset://textures/universalx2.png")
            ?.AddCallbackForSuccess(x => universalSurfaceTexture = gameRenderer.GameManager.GameAssetManager.LoadTextureFromPath(x.LocalDownloadPath!));
        gameRenderer.GameManager.GameAssetManager.QuickLoad("rbxasset://textures/kriscrossapplesaucex2.png")
            ?.AddCallbackForSuccess(x => glueSurfaceTexture = gameRenderer.GameManager.GameAssetManager.LoadTextureFromPath(x.LocalDownloadPath!));

        gameRenderer.GameManager.GameAssetManager.QuickLoad("rbxasset://textures/studx2normal.png")
            ?.AddCallbackForSuccess(x => studSurfaceNormalTexture = gameRenderer.GameManager.GameAssetManager.LoadTextureFromPath(x.LocalDownloadPath!));
        gameRenderer.GameManager.GameAssetManager.QuickLoad("rbxasset://textures/inletx2normal.png")
            ?.AddCallbackForSuccess(x => inletSurfaceNormalTexture = gameRenderer.GameManager.GameAssetManager.LoadTextureFromPath(x.LocalDownloadPath!));
        gameRenderer.GameManager.GameAssetManager.QuickLoad("rbxasset://textures/universalx2normal.png")
            ?.AddCallbackForSuccess(x => universalSurfaceNormalTexture = gameRenderer.GameManager.GameAssetManager.LoadTextureFromPath(x.LocalDownloadPath!));
        gameRenderer.GameManager.GameAssetManager.QuickLoad("rbxasset://textures/kriscrossapplesaucex2normal.png")
            ?.AddCallbackForSuccess(x => glueSurfaceNormalTexture = gameRenderer.GameManager.GameAssetManager.LoadTextureFromPath(x.LocalDownloadPath!));
        
        ShadowMap = CreateShadowmap(4096, 4096);
    }

    // a code from a better experiment

    internal void ApplyPartSurfaceMaterial(PartSpecification specification, bool useSurfaces)
    {
        SetIntUniform(uniform_instanced_surfaceTop, (int)specification.TopSurface);
        SetIntUniform(uniform_instanced_surfaceBottom, (int)specification.BottomSurface);
        SetIntUniform(uniform_instanced_surfaceLeft, (int)specification.LeftSurface);
        SetIntUniform(uniform_instanced_surfaceRight, (int)specification.RightSurface);
        SetIntUniform(uniform_instanced_surfaceFront, (int)specification.FrontSurface);
        SetIntUniform(uniform_instanced_surfaceBack, (int)specification.BackSurface);
        SetIntUniform(uniform_instanced_useSurfaces, useSurfaces ? 1 : 0);

        SetVector4Uniform(uniform_instanced_decalBackTint, new(specification.BackDecal.DecalTint.ToVector3(), 1 - specification.BackDecal.DecalTransparency));
        SetVector4Uniform(uniform_instanced_decalBottomTint, new(specification.BottomDecal.DecalTint.ToVector3(), 1 - specification.BottomDecal.DecalTransparency));
        SetVector4Uniform(uniform_instanced_decalRightTint, new(specification.RightDecal.DecalTint.ToVector3(), 1 - specification.RightDecal.DecalTransparency));
        SetVector4Uniform(uniform_instanced_decalLeftTint, new(specification.LeftDecal.DecalTint.ToVector3(), 1 - specification.LeftDecal.DecalTransparency));
        SetVector4Uniform(uniform_instanced_decalFrontTint, new(specification.FrontDecal.DecalTint.ToVector3(), 1 - specification.FrontDecal.DecalTransparency));
        SetVector4Uniform(uniform_instanced_decalTopTint, new(specification.TopDecal.DecalTint.ToVector3(), 1 - specification.TopDecal.DecalTransparency));

        if (specification.BackDecal.DecalTexture.Id > 0)
            BindSurfaceTexture(uniform_instanced_decalBack, specification.BackDecal.DecalTexture, 9);
        if (specification.FrontDecal.DecalTexture.Id > 0)
            BindSurfaceTexture(uniform_instanced_decalFront, specification.FrontDecal.DecalTexture, 10);
        if (specification.TopDecal.DecalTexture.Id > 0)
            BindSurfaceTexture(uniform_instanced_decalTop, specification.TopDecal.DecalTexture, 11);
        if (specification.BottomDecal.DecalTexture.Id > 0)
            BindSurfaceTexture(uniform_instanced_decalBottom, specification.BottomDecal.DecalTexture, 12);
        if (specification.LeftDecal.DecalTexture.Id > 0)
            BindSurfaceTexture(uniform_instanced_decalLeft, specification.LeftDecal.DecalTexture, 13);
        if (specification.RightDecal.DecalTexture.Id > 0)
            BindSurfaceTexture(uniform_instanced_decalRight, specification.RightDecal.DecalTexture, 14);
    }

    private unsafe void SetIntUniform(int location, int value)
    {
        if (location != -1)
            Raylib.SetShaderValue(SpecularLightingInstancedShader, location, &value, ShaderUniformDataType.Int);
    }
    private unsafe void SetVector4Uniform(int location, Vector4 value)
    {
        if (location != -1)
            Raylib.SetShaderValue(SpecularLightingInstancedShader, location, &value, ShaderUniformDataType.Vec4);
    }
    private void BindSurfaceTexture(int location, Texture2D texture, int textureUnit)
    {
        SetIntUniform(location, textureUnit);

        if (texture.Id == 0)
            texture = blankSurfaceTexture;

        Rlgl.ActiveTextureSlot(textureUnit);
        if (texture.Id != 0)
            Rlgl.EnableTexture(texture.Id);
        else
            Rlgl.DisableTexture();
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
            ", instance count: " + GameRenderer.GameManager.GameRegistry.GetInstanceCount() + 
            ", part render buffer category count: " + (opaquePartRenderBuffer.Count + transparentPartRenderBuffer.Count);
        
        NetworkClient? client = GameRenderer.Root.FindService<NetworkClient>();

        if (client != null)
            value += ", ping: " + client.LastServerPingValue.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) + " ms";

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
    public virtual void RenderParticles()
    {
        foreach (ParticleStream particleStream in ParticleStreams)
            particleStream.StepAndRenderAllParticles();
    }
    public override void RenderFrame()
    {
        Raylib.ClearBackground(Color.SkyBlue);

        CoreEditorService? coreEditorService = GameRenderer.GameManager.RootModel.FindService<CoreEditorService>();
        Lighting lighting = GameRenderer.GameManager.RootModel.GetService<Lighting>();

        lastFrustum = Frustum.ExtractFrustum(0.01f, 1000, MainCamera);

        if (lighting.RealTimeShadows)
        {
            LightCamera = new Camera3D()
            {
                Projection = CameraProjection.Orthographic,
                Position = GameRenderer.GameManager.RootModel.GetService<Lighting>().SunPosition * 20 + MainCamera.Position,
                Target = MainCamera.Position,
                Up = Vector3.UnitY,
                FovY = 320
            };

            float snap = LightCamera.FovY * 10 / ShadowMap.Depth.Height;
            LightCamera.Position = LightCamera.Position.Snap(snap);
            LightCamera.Target = LightCamera.Target.Snap(snap);

            float shadowMapAspect = ShadowMap.Texture.Width / (float)ShadowMap.Texture.Height;
            Matrix4x4 matlightVP = Raymath.MatrixMultiply(
                Raylib.GetCameraViewMatrix(ref LightCamera),
                Raylib.GetCameraProjectionMatrix(ref LightCamera, shadowMapAspect));

            Raylib.SetShaderValueMatrix(SpecularLightingShader, uniform_lightVP, matlightVP);
            Raylib.SetShaderValueMatrix(SpecularLightingInstancedShader, uniform_instanced_lightVP, matlightVP);
        }

        if (coreEditorService != null)
        {
            if (!coreEditorService.KeyboardFocused)
                DoDebugKeys();
        }
        else
            DoDebugKeys();

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

        GameRenderer.GameManager.TryGetEventForId(GameEvent.EVENT_RENDER3D)?.Fire(RenderingEventArgs);

        if (lighting.RealTimeShadows)
        {
            Raylib.EndMode3D();
            Raylib.BeginTextureMode(ShadowMap);
            Raylib.BeginMode3D(LightCamera);

            Raylib.ClearBackground(Color.Black);

            DrawPartRenderBuffer(shadowPass: true);

            Raylib.EndMode3D();
            Raylib.EndTextureMode();
            Raylib.BeginMode3D(MainCamera);
        }


        BindSurfaceTexture(uniform_instanced_blankTexture, blankSurfaceTexture, 0);
        BindSurfaceTexture(uniform_instanced_studTexture, studSurfaceTexture, 1);
        BindSurfaceTexture(uniform_instanced_inletTexture, inletSurfaceTexture, 2);
        BindSurfaceTexture(uniform_instanced_universalTexture, universalSurfaceTexture, 3);
        BindSurfaceTexture(uniform_instanced_glueTexture, glueSurfaceTexture, 4);
        BindSurfaceTexture(uniform_instanced_studNormalTexture, studSurfaceNormalTexture, 5);
        BindSurfaceTexture(uniform_instanced_inletNormalTexture, inletSurfaceNormalTexture, 6);
        BindSurfaceTexture(uniform_instanced_universalNormalTexture, universalSurfaceNormalTexture, 7);
        BindSurfaceTexture(uniform_instanced_glueNormalTexture, glueSurfaceNormalTexture, 8);
        
        SetIntUniform(uniform_instanced_shadowmap, 15);
        Rlgl.ActiveTextureSlot(15);
        Rlgl.EnableTexture(ShadowMap.Depth.Id);
        Rlgl.ActiveTextureSlot(0);

        DrawPartRenderBuffer();
        RenderParticles();

        if (GameRenderer.DebugFlag)
            DrawGizmos(true);

        Raylib.EndMode3D();

        if (GameRenderer.DebugFlag)
            DrawGizmos(false);

        GameRenderer.GameManager.TryGetEventForId(GameEvent.EVENT_RENDERGUI_LEVEL0)?.Fire(RenderingEventArgs);
        GameRenderer.GameManager.TryGetEventForId(GameEvent.EVENT_RENDERGUI_LEVEL1)?.Fire(RenderingEventArgs);
        GameRenderer.GameManager.TryGetEventForId(GameEvent.EVENT_RENDERGUI_LEVEL2)?.Fire(RenderingEventArgs);
        GameRenderer.GameManager.TryGetEventForId(GameEvent.EVENT_RENDERGUI_LEVEL3)?.Fire(RenderingEventArgs);
        GameRenderer.GameManager.TryGetEventForId(GameEvent.EVENT_RENDERGUI_LEVEL4)?.Fire(RenderingEventArgs);

        if (GameRenderer.DebugFlag)
            RenderDebugInfo();

        opaquePartRenderBuffer.Clear();
        transparentPartRenderBuffer.Clear();
        maxPartRenderBufferDepth = 0;

        // Raylib.DrawTexture(ShadowMap.Texture, 0, 0, Color.White);
    }
    public virtual void DrawPartRenderBuffer(bool shadowPass = false)
    {
        if (sphereMesh.VaoId == 0)
            sphereMesh = Raylib.GenMeshSphere(0.5f, 9, 15);
        if (cubeMesh.VaoId == 0)
            cubeMesh = Raylib.GenMeshCube(1, 1, 1);

        void DoRenderABuffer(Dictionary<PartSpecification, List<BasePart>> renderBuffer)
        {
            if (renderInstances == null)
                renderInstances = new PartRenderInstanceInfo[maxPartRenderBufferDepth];
            if (renderInstances != null && renderInstances.Length < maxPartRenderBufferDepth)
                Array.Resize(ref renderInstances, maxPartRenderBufferDepth);
            if (renderInstances == null)
                throw new OutOfMemoryException("No memory for render instance list");

            foreach (KeyValuePair<PartSpecification, List<BasePart>> kvp in renderBuffer)
            {
                PartSpecification partSpecification = kvp.Key;

                if (partSpecification.Shape == PartType.Block)
                {
                    int j = 0;
                    for (int i = 0; i < kvp.Value.Count; i++)
                    {
                        BasePart basePart = kvp.Value[i];
                        if ((!basePart.CastShadow || basePart.Transparency > 0.75f) && shadowPass)
                            continue;
                        if (!shadowPass && basePart.Root.GetService<Lighting>().RealTimeFrustumCulling)
                        {
                            basePart.CalculateBoundingBox(out Vector3 min, out Vector3 max);
                            if (!lastFrustum.IsInFrustum(min, max))
                                continue;
                        }

                        Matrix4x4 matrix = Matrix4x4.CreateScale(basePart.Size.X, basePart.Size.Y, basePart.Size.Z);
                        matrix *= Matrix4x4.CreateFromQuaternion(basePart.QuaternionRotation);
                        matrix *= Matrix4x4.CreateTranslation(basePart.Position.X, basePart.Position.Y, basePart.Position.Z);
                        matrix = Matrix4x4.Transpose(matrix);
                        renderInstances[j].Transform = Raymath.MatrixToFloatV(matrix);
                        renderInstances[j].Color.X = basePart.Color3.R / 255f;
                        renderInstances[j].Color.Y = basePart.Color3.G / 255f;
                        renderInstances[j].Color.Z = basePart.Color3.B / 255f;
                        renderInstances[j].Color.W = 1 - basePart.Transparency;

                        j++;
                    }

                    RenderUtils.CustomDrawMeshInstanced(cubeMesh, renderInstances.AsSpan()[..j], this, partSpecification, true, shadowPass);
                }
                else if (partSpecification.Shape == PartType.Ball)
                {
                    int j = 0;
                    for (int i = 0; i < kvp.Value.Count; i++)
                    {
                        BasePart basePart = kvp.Value[i];
                        if ((!basePart.CastShadow || basePart.Transparency > 0.75f) && shadowPass)
                            continue;
                        if (!shadowPass && basePart.Root.GetService<Lighting>().RealTimeFrustumCulling)
                        {
                            basePart.CalculateBoundingBox(out Vector3 min, out Vector3 max);
                            if (!lastFrustum.IsInFrustum(min, max))
                                continue;
                        }

                        Vector3 size = basePart.Size;
                        float minsize = size.X;

                        if (size.Y < minsize)
                            minsize = size.Y;
                        if (size.Z < minsize)
                            minsize = size.Z;

                        Matrix4x4 matrix = Matrix4x4.CreateScale(minsize, minsize, minsize);
                        matrix *= Matrix4x4.CreateFromQuaternion(basePart.QuaternionRotation);
                        matrix *= Matrix4x4.CreateTranslation(basePart.Position.X, basePart.Position.Y, basePart.Position.Z);
                        matrix = Matrix4x4.Transpose(matrix);
                        renderInstances[j].Transform = Raymath.MatrixToFloatV(matrix);
                        renderInstances[j].Color.X = basePart.Color3.R / 255f;
                        renderInstances[j].Color.Y = basePart.Color3.G / 255f;
                        renderInstances[j].Color.Z = basePart.Color3.B / 255f;
                        renderInstances[j].Color.W = 1 - basePart.Transparency;

                        j++;
                    }

                    RenderUtils.CustomDrawMeshInstanced(sphereMesh, renderInstances.AsSpan()[..j], this, partSpecification, false, shadowPass);
                }
            }
        }

        DoRenderABuffer(opaquePartRenderBuffer);

        Rlgl.DisableDepthMask();
        DoRenderABuffer(transparentPartRenderBuffer);
        Rlgl.EnableDepthMask();
    }
    public virtual void DoDebugKeys()
    {
        if (Raylib.IsMouseButtonPressed(MouseButton.Right))
            Raylib.DisableCursor();

        if (Raylib.IsMouseButtonDown(MouseButton.Right) || followHumanoid != null)
        {
            Vector3 position = MainCamera.Position;
            Vector3 target = MainCamera.Target;

            // an already slow update loop, slowified even more.
            for (int i = 0; i < SpeedMultiplier; i++)
            {
                Raylib.UpdateCamera(ref MainCamera, followHumanoid == null ? CameraMode.FirstPerson : CameraMode.ThirdPerson);
                Raylib.UpdateCamera(ref MainCamera, followHumanoid == null ? CameraMode.FirstPerson : CameraMode.ThirdPerson);
            }

            if (followHumanoid != null)
            {
                Vector3 difference = MainCamera.Position - MainCamera.Target;
                Model? model = followHumanoid.Parent as Model;
                if (model != null)
                {
                    BasePart? head = model.FindFirstChild_Fast("Head") as BasePart;
                    if (head != null)
                    {
                        MainCamera.Target = head.Position;
                        MainCamera.Position = difference + MainCamera.Target;
                    }
                }
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
        if (Raylib.IsKeyPressed(KeyboardKey.B))
        {
            Part part = GameRenderer.GameManager.GameRegistry.Construct<Part>();
            part.Size = new Vector3(0.75f, 0.75f, 0.75f);
            part.BrickColor = BrickColor.Blue;
            part.Anchored = false;
            part.Position = MainCamera.Position;
            part.Shape = PartType.Ball;
            part.Velocity = Vector3.Normalize(MainCamera.Target - MainCamera.Position) * 14 * SpeedMultiplier;
            if (Raylib.IsKeyDown(KeyboardKey.LeftAlt))
                part.Shape = PartType.Ball;

            part.Parent = GameRenderer.GameManager.RootModel.GetService<Workspace>();

            if (Raylib.IsKeyDown(KeyboardKey.LeftAlt))
            {
                part.BrickColor = BrickColor.Red;
                ScriptContext scriptContext = GameRenderer.Root.GetService<ScriptContext>();
                Script script = scriptContext.GetLuaStateFor(SecurityIdentity.SI_ElevatedGameScript);

                LuaEventConnection? connection = part.Touched.Connect(part.GameManager, SecurityIdentity.SI_ElevatedGameScript, DynValue.NewCallback((_, args) =>
                {
                    InstanceBridge.InstanceBridgeDescriptor? otherPartD = args[0].UserData.Descriptor as InstanceBridge.InstanceBridgeDescriptor;
                    if (otherPartD == null)
                        return DynValue.Void;
                    BasePart? basePart = GameRenderer.GameManager.GameRegistry.GetLocalInstanceById(otherPartD.Value) as BasePart;
                    if (basePart == null)
                        return DynValue.Void;
                    basePart.Destroy();
                    part.Destroy();
                    return DynValue.Void;
                }), null);
            }

            GameRenderer.GameManager.RootModel.GetService<Debris>().AddItem(part, 5);
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
        if (Raylib.IsKeyPressed(KeyboardKey.F))
        {
            ParticleStream stream = new ParticleStream(
                this, ParticleStreamMode.Fire, null, 100
            );
            stream.Position = MainCamera.Target;
            ParticleStreams.Add(stream);
        }
        if (Raylib.IsKeyPressed(KeyboardKey.V))
        {
            for (int i = 0; i < 300; i++)
            {
                Part part = GameRenderer.GameManager.GameRegistry.Construct<Part>();
                part.Size = new Vector3(2, 2, 4);
                part.BrickColor = BrickColor.Random();
                part.Anchored = false;
                part.Position = MainCamera.Position + new Vector3(Random.Shared.Next(-7, 7), Random.Shared.Next(0, 15), Random.Shared.Next(-7, 7));
                if (Raylib.IsKeyDown(KeyboardKey.LeftAlt))
                    part.Shape = PartType.Ball;

                part.Parent = GameRenderer.GameManager.RootModel.GetService<Workspace>();
            }
        }
        if (Raylib.IsKeyPressed(KeyboardKey.I))
        {
            Raylib.CameraMoveToTarget(ref MainCamera, 1f / GameRenderer.PreferredFPS);
        }
        if (Raylib.IsKeyPressed(KeyboardKey.O))
        {
            Raylib.CameraMoveToTarget(ref MainCamera, -1f / GameRenderer.PreferredFPS);
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
        if (Raylib.IsKeyPressed(KeyboardKey.H) && followHumanoid == null)
        {
            Part humanoidRootPart = GameRenderer.GameManager.GameRegistry.Construct<Part>();
            Part torso = GameRenderer.GameManager.GameRegistry.Construct<Part>();
            Part leftLeg = GameRenderer.GameManager.GameRegistry.Construct<Part>();
            Part rightLeg = GameRenderer.GameManager.GameRegistry.Construct<Part>();
            Part leftArm = GameRenderer.GameManager.GameRegistry.Construct<Part>();
            Part rightArm = GameRenderer.GameManager.GameRegistry.Construct<Part>();
            Part head = GameRenderer.GameManager.GameRegistry.Construct<Part>();
            Humanoid humanoid = GameRenderer.GameManager.GameRegistry.Construct<Humanoid>();
            Model model = GameRenderer.GameManager.GameRegistry.Construct<Model>();

            Decal decal = GameRenderer.GameManager.GameRegistry.Construct<Decal>();

            decal.Name = "face";
            decal.Parent = head;
            decal.Content = "rbxasset://textures/smile.png";

            humanoidRootPart.Name = "HumanoidRootPart";
            humanoidRootPart.Transparency = 1;
            humanoidRootPart.CanCollide = true;
            humanoidRootPart.Parent = model;
            humanoidRootPart.Size = new Vector3(1, 5, 2);
            humanoidRootPart.Position = new Vector3(0, -0.5f, 0);

            torso.Name = "Torso";
            torso.Parent = model;
            torso.Size = new Vector3(1, 2, 2);
            torso.BrickColor = BrickColor.Random();
            torso.CanCollide = false;

            leftArm.Name = "Left Arm";
            leftArm.Parent = model;
            leftArm.Size = new Vector3(1, 2, 1);
            leftArm.Position = new Vector3(0, 0, 1.5f);
            leftArm.BrickColor = BrickColor.Random();
            leftArm.CanCollide = false;

            rightArm.Name = "Right Arm";
            rightArm.Parent = model;
            rightArm.Size = new Vector3(1, 2, 1);
            rightArm.Position = new Vector3(0, 0, -1.5f);
            rightArm.BrickColor = leftArm.BrickColor;
            rightArm.CanCollide = false;

            leftLeg.Name = "Left Leg";
            leftLeg.Parent = model;
            leftLeg.Size = new Vector3(1, 2, 1);
            leftLeg.Position = new Vector3(0, -2, 0.5f);
            leftLeg.BrickColor = BrickColor.Random();
            leftLeg.CanCollide = false;

            rightLeg.Name = "Right Leg";
            rightLeg.Parent = model;
            rightLeg.Size = new Vector3(1, 2, 1);
            rightLeg.Position = new Vector3(0, -2, -0.5f);
            rightLeg.BrickColor = leftLeg.BrickColor;
            rightLeg.CanCollide = false;

            head.Name = "Head";
            head.Parent = model;
            head.Shape = PartType.Ball;
            head.Size = new Vector3(1.5f, 1.5f, 1.5f);
            head.Position = new Vector3(0, 1.7f, 0);
            head.BrickColor = BrickColor.Random();
            head.CanCollide = false;

            Weld weld;

            weld = GameRenderer.GameManager.GameRegistry.Construct<Weld>();
            weld.Part0 = torso;
            weld.Part1 = humanoidRootPart;
            weld.Parent = humanoidRootPart;
            weld = GameRenderer.GameManager.GameRegistry.Construct<Weld>();
            weld.Part0 = leftArm;
            weld.Part1 = humanoidRootPart;
            weld.Parent = humanoidRootPart;
            weld = GameRenderer.GameManager.GameRegistry.Construct<Weld>();
            weld.Part0 = rightArm;
            weld.Part1 = humanoidRootPart;
            weld.Parent = humanoidRootPart;
            weld = GameRenderer.GameManager.GameRegistry.Construct<Weld>();
            weld.Part0 = leftLeg;
            weld.Part1 = humanoidRootPart;
            weld.Parent = humanoidRootPart;
            weld = GameRenderer.GameManager.GameRegistry.Construct<Weld>();
            weld.Part0 = rightLeg;
            weld.Part1 = humanoidRootPart;
            weld.Parent = humanoidRootPart;
            weld = GameRenderer.GameManager.GameRegistry.Construct<Weld>();
            weld.Part0 = head;
            weld.Part1 = humanoidRootPart;
            weld.Parent = humanoidRootPart;

            humanoid.Parent = model;
            followHumanoid = humanoid;

            model.Parent = GameRenderer.GameManager.RootModel.GetService<Workspace>();
            model.PrimaryPart = humanoidRootPart;

            model.TranslateBy(MainCamera.Position);
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
            shutdownButton.Size = new UDim2(0, 240, 0, 25);
            shutdownButton.Position = new UDim2(0.5f, 0, 0.8f, 0);
            shutdownButton.AnchorPoint = new Vector2(0.5f, 0.5f);
            shutdownButton.SetVerb("shutdown");

            TextButton copyTicketButton = GameRenderer.GameManager.GameRegistry.Construct<TextButton>();
            copyTicketButton.Parent = screenGui;
            copyTicketButton.Text = "Copy Server Ticket (public)";
            copyTicketButton.Size = new UDim2(0, 240, 0, 25);
            copyTicketButton.Position = new UDim2(0.5f, 0, 0.8f, 27);
            copyTicketButton.AnchorPoint = new Vector2(0.5f, 0.5f);
            copyTicketButton.SetVerb("copyTicket");

            TextButton copyTicketLOButton = GameRenderer.GameManager.GameRegistry.Construct<TextButton>();
            copyTicketLOButton.Parent = screenGui;
            copyTicketLOButton.Text = "Copy Server Ticket (LAN-only)";
            copyTicketLOButton.Size = new UDim2(0, 240, 0, 25);
            copyTicketLOButton.Position = new UDim2(0.5f, 0, 0.8f, 54);
            copyTicketLOButton.AnchorPoint = new Vector2(0.5f, 0.5f);
            copyTicketLOButton.SetVerb("copyTicketLO");

            TextButton copyTicketLHButton = GameRenderer.GameManager.GameRegistry.Construct<TextButton>();
            copyTicketLHButton.Parent = screenGui;
            copyTicketLHButton.Text = "Copy Server Ticket (localhost-only)";
            copyTicketLHButton.Size = new UDim2(0, 240, 0, 25);
            copyTicketLHButton.Position = new UDim2(0.5f, 0, 0.8f, 81);
            copyTicketLHButton.AnchorPoint = new Vector2(0.5f, 0.5f);
            copyTicketLHButton.SetVerb("copyTicketLH");

            TextButton doNothingButton = GameRenderer.GameManager.GameRegistry.Construct<TextButton>();
            doNothingButton.Parent = screenGui;
            doNothingButton.Text = "Do nothing";
            doNothingButton.Size = new UDim2(0, 240, 0, 25);
            doNothingButton.Position = new UDim2(0.5f, 0, 0.8f, 108);
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
        else if (Raylib.IsKeyPressed(KeyboardKey.Five))
        {
            SpeedMultiplier = 5;
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Six))
        {
            SpeedMultiplier = 6;
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Eight))
        {
            CoreEditorService coreEditorService = GameRenderer.Root.GetService<CoreEditorService>();
            coreEditorService.ToggleEditorMode();
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Seven))
        {
            DisableGizmos = !DisableGizmos;
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Nine))
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

            ScriptContext scriptContext = GameRenderer.Root.GetService<ScriptContext>();
            Script script = scriptContext.GetLuaStateFor(SecurityIdentity.SI_ElevatedGameScript);

            LuaEventConnection? connection = part.Touched.Connect(part.GameManager, SecurityIdentity.SI_ElevatedGameScript, DynValue.NewCallback((_, args) =>
            {
                part.BrickColor = BrickColor.Random();
                return DynValue.Void;
            }), null);

            part.Parent = GameRenderer.GameManager.RootModel.GetService<Workspace>();
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
    }
    public virtual void DrawGizmos(bool in3d)
    {
        if (DisableGizmos)
            return;

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
            {
                Workspace workspace = GameRenderer.Root.GetService<Workspace>();
                TraceResult result = workspace.Trace(MainCamera.Position, (MainCamera.Target - MainCamera.Position) * 200 + MainCamera.Position, null);
                Vector3 fp = (result.End - result.Start) * result.Fraction + result.Start;

                Raylib.DrawSphere(fp, 0.25f, Color.Magenta);
                Raylib.DrawSphere(fp + result.Normal, 0.25f, Color.Magenta);
            }

            if (in3d)
                Raylib.EndShaderMode();

            if (in3d)
            {
                Rlgl.DisableDepthMask();
                Rlgl.DisableDepthTest();

                Raylib.DrawLine3D(MainCamera.Target, MainCamera.Target + Vector3.UnitX, Color.Red);
                Raylib.DrawLine3D(MainCamera.Target, MainCamera.Target + Vector3.UnitY, Color.Green);
                Raylib.DrawLine3D(MainCamera.Target, MainCamera.Target + Vector3.UnitZ, Color.Blue);

                Rlgl.EnableDepthTest();
                Rlgl.EnableDepthMask();
            }

            if (in3d)
                GameRenderer.GameManager.TryGetEventForId(GameEvent.EVENT_RENDERGIZMOS)?.Fire();
        }
    }
    public void WritePart(BasePart part)
    {
        PartSpecification partSpecification = default;

        if (part.Root.GetService<Lighting>().RealTimeFrustumCulling)
        {
            part.CalculateBoundingBox(out Vector3 min, out Vector3 max);
            if (!lastFrustum.IsInFrustum(min, max))
                return;
        }

        if (part is Part concretePart)
        {
            partSpecification.Shape = concretePart.Shape;
            partSpecification.TopSurface = concretePart.TopSurface;
            partSpecification.LeftSurface = concretePart.LeftSurface;
            partSpecification.RightSurface = concretePart.RightSurface;
            partSpecification.BottomSurface = concretePart.BottomSurface;
            partSpecification.FrontSurface = concretePart.FrontSurface;
            partSpecification.BackSurface = concretePart.BackSurface;

            Decal? instance = null;

            if (concretePart.lastDecalTextureBackInstance != null && concretePart.lastDecalTextureBackInstance.TryGetTarget(out instance))
            {
                if (concretePart.lastDecalTextureBackLoaded != null)
                    partSpecification.BackDecal.DecalTexture = concretePart.lastDecalTextureBackLoaded.Texture2D;
                partSpecification.BackDecal.DecalTransparency = instance.Transparency;
                partSpecification.BackDecal.DecalTint = instance.Color3;
            }

            if (concretePart.lastDecalTextureFrontInstance != null && concretePart.lastDecalTextureFrontInstance.TryGetTarget(out instance))
            {
                if (concretePart.lastDecalTextureFrontLoaded != null)
                    partSpecification.FrontDecal.DecalTexture = concretePart.lastDecalTextureFrontLoaded.Texture2D;
                partSpecification.FrontDecal.DecalTransparency = instance.Transparency;
                partSpecification.FrontDecal.DecalTint = instance.Color3;
            }

            if (concretePart.lastDecalTextureLeftInstance != null && concretePart.lastDecalTextureLeftInstance.TryGetTarget(out instance))
            {
                if (concretePart.lastDecalTextureLeftLoaded != null)
                    partSpecification.LeftDecal.DecalTexture = concretePart.lastDecalTextureLeftLoaded.Texture2D;
                partSpecification.LeftDecal.DecalTransparency = instance.Transparency;
                partSpecification.LeftDecal.DecalTint = instance.Color3;
            }

            if (concretePart.lastDecalTextureRightInstance != null && concretePart.lastDecalTextureRightInstance.TryGetTarget(out instance))
            {
                if (concretePart.lastDecalTextureRightLoaded != null)
                    partSpecification.RightDecal.DecalTexture = concretePart.lastDecalTextureRightLoaded.Texture2D;
                partSpecification.RightDecal.DecalTransparency = instance.Transparency;
                partSpecification.RightDecal.DecalTint = instance.Color3;
            }

            if (concretePart.lastDecalTextureTopInstance != null && concretePart.lastDecalTextureTopInstance.TryGetTarget(out instance))
            {
                if (concretePart.lastDecalTextureTopLoaded != null)
                    partSpecification.TopDecal.DecalTexture = concretePart.lastDecalTextureTopLoaded.Texture2D;
                partSpecification.TopDecal.DecalTransparency = instance.Transparency;
                partSpecification.TopDecal.DecalTint = instance.Color3;
            }

            if (concretePart.lastDecalTextureBottomInstance != null && concretePart.lastDecalTextureBottomInstance.TryGetTarget(out instance))
            {
                if (concretePart.lastDecalTextureBottomLoaded != null)
                    partSpecification.BottomDecal.DecalTexture = concretePart.lastDecalTextureBottomLoaded.Texture2D;
                partSpecification.BottomDecal.DecalTransparency = instance.Transparency;
                partSpecification.BottomDecal.DecalTint = instance.Color3;
            }
        }
        else
        {
            throw new NotImplementedException("Unsupported part type");
        }

        if (part.Transparency <= 0)
        {
            if (!opaquePartRenderBuffer.TryGetValue(partSpecification, out List<BasePart>? list))
            {
                list = [];
                opaquePartRenderBuffer[partSpecification] = list;
            }
            
            list.Add(part);
            if (list.Count > maxPartRenderBufferDepth)
                maxPartRenderBufferDepth = list.Count;
        }
        else if (part.Transparency < 1)
        {
            if (!transparentPartRenderBuffer.TryGetValue(partSpecification, out List<BasePart>? list))
            {
                list = [];
                transparentPartRenderBuffer[partSpecification] = list;
            }
            
            list.Add(part);
            if (list.Count > maxPartRenderBufferDepth)
                maxPartRenderBufferDepth = list.Count;
        }
        // so if the transparency is 1 it doesnt get put into the render buffer at all
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
            target.Texture.Id = Rlgl.LoadTexture((void*)0, width, height, PixelFormat.UncompressedR8G8B8, 1);
            target.Texture.Width = width;
            target.Texture.Height = height;
            target.Texture.Format = PixelFormat.UncompressedR8G8B8; // encode colour as 4 8 bit floats
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