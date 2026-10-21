using System.Numerics;
using NetBlox.Instances;
using NetBlox.Structs;
using Raylib_cs;

namespace NetBlox.Rendering;

public struct ParticleState
{
    public bool Alive;
    public Vector3 WorldPosition;
    public Vector3 WorldVelocity;
    public Color3 ParticleTint;
    public float ParticleScale;
    public float ParticleTransparency;
    public float ParticleRotation;
    public int ParticleLifetime;
    public Texture2D ParticleTexture;
}

public enum ParticleStreamMode
{
    Fire, Smoke, ForceField
}

public class ParticleStream
{
    public int ParticleLimit;
    public ParticleStreamMode Mode;
    public Vector3 Position;
    public readonly WorkspaceRendererViewport Viewport;
    public readonly Instance? EffectProvider;
    public readonly ParticleState[] AllParticles;

    private bool firstRender = true;

    public ParticleStream(WorkspaceRendererViewport viewport, ParticleStreamMode mode, Instance? effectProvider, int maxParticles)
    {
        Viewport = viewport;
        EffectProvider = effectProvider;
        ParticleLimit = maxParticles;
        Mode = mode;
        AllParticles = new ParticleState[maxParticles];
    }

    public void StepAndRenderAllParticles()
    {
        int maxParticles = AllParticles.Length;
        if (ParticleLimit < maxParticles)
            maxParticles = ParticleLimit;

        Vector3 cameraForward = Vector3.Normalize(Viewport.MainCamera.Target - Viewport.MainCamera.Position);
        Vector3 idealLookat = Vector3.Normalize(Position - Viewport.MainCamera.Position);

        if (Vector3.Dot(cameraForward, idealLookat) < 0)
            return;

        Rlgl.DisableDepthMask();
        Rlgl.SetBlendMode(BlendMode.Multiplied);

        Vector3 cameraTrueUp = cameraForward;
        cameraTrueUp = Vector3.Normalize(Vector3.Cross(cameraTrueUp, Viewport.MainCamera.Up));
        cameraTrueUp = Vector3.Normalize(Vector3.Cross(cameraTrueUp, cameraForward));

        if (firstRender)
        {
            firstRender = false;
            for (int i = 0; i < AllParticles.Length; i++)
            {
                AllParticles[i].WorldPosition = Position;
                AllParticles[i].WorldVelocity = new Vector3(
                    (Random.Shared.NextSingle() - 0.5f) / Viewport.GameRenderer.PreferredFPS * 1.3f,
                    4f / Viewport.GameRenderer.PreferredFPS, 
                    (Random.Shared.NextSingle() - 0.5f) / Viewport.GameRenderer.PreferredFPS * 1.3f);
                AllParticles[i].ParticleLifetime = Random.Shared.Next(0, (int)(Viewport.GameRenderer.PreferredFPS * 1.5f));
                AllParticles[i].ParticleRotation = Random.Shared.NextSingle() * 360;
            }
        }
        
        if (Mode == ParticleStreamMode.Fire)
        {
            for (int i = 0; i < maxParticles; i++)
            {
                AllParticles[i].ParticleTexture = Viewport.ParticleFireTexture;
                AllParticles[i].WorldPosition += AllParticles[i].WorldVelocity;
                AllParticles[i].WorldVelocity *= 0.95f;
                AllParticles[i].ParticleLifetime++;

                if (AllParticles[i].ParticleLifetime > Viewport.GameRenderer.PreferredFPS * 1f)
                {
                    AllParticles[i].ParticleTransparency = 
                        (AllParticles[i].ParticleLifetime - Viewport.GameRenderer.PreferredFPS * 1f) * 2;
                }
                if (AllParticles[i].ParticleLifetime > Viewport.GameRenderer.PreferredFPS * 1.5f)
                {
                    AllParticles[i].WorldPosition = Position;
                    AllParticles[i].WorldVelocity = new Vector3(
                        (Random.Shared.NextSingle() - 0.5f) / Viewport.GameRenderer.PreferredFPS * 1.3f, 
                        4f / Viewport.GameRenderer.PreferredFPS, 
                        (Random.Shared.NextSingle() - 0.5f) / Viewport.GameRenderer.PreferredFPS * 1.3f);
                    AllParticles[i].ParticleLifetime = 0;
                    AllParticles[i].ParticleRotation = Random.Shared.NextSingle() * 360;
                }

                Color tint = Color.White;
                tint.A = (byte)((1 - AllParticles[i].ParticleTransparency) * 255);
                Raylib.DrawBillboardPro(Viewport.MainCamera, 
                    AllParticles[i].ParticleTexture, 
                    new Raylib_cs.Rectangle(0, 0, AllParticles[i].ParticleTexture.Width, AllParticles[i].ParticleTexture.Height),
                    AllParticles[i].WorldPosition, cameraTrueUp, new Vector2(1, 1), new Vector2(0.5f, 0.5f), AllParticles[i].ParticleRotation, tint);
            }
        }

        Rlgl.SetBlendMode(BlendMode.Alpha);
        Rlgl.EnableDepthMask();

        // TODO: this
    }
}