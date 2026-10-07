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
    public float ParticleLifetime;
}

public enum ParticleStreamMode
{
    Fire, Smoke, ForceField
}

public class ParticleStream
{
    public int ParticleLimit;
    public ParticleStreamMode Mode;
    public readonly WorkspaceRendererViewport Viewport;
    public readonly Instance EffectProvider;
    public readonly ParticleState[] AllParticles;

    public ParticleStream(WorkspaceRendererViewport viewport, Instance effectProvider, int maxParticles)
    {
        Viewport = viewport;
        EffectProvider = effectProvider;
        ParticleLimit = maxParticles;
        AllParticles = new ParticleState[maxParticles];
    }

    public void StepAndRenderAllParticles()
    {
        int maxParticles = AllParticles.Length;
        if (ParticleLimit < maxParticles)
            maxParticles = ParticleLimit;
        
        for (int i = 0; i < maxParticles; i++)
        {
        }

        // TODO: this
    }
}