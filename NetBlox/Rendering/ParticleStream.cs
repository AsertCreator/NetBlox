using System.Numerics;
using NetBlox.Instances;
using NetBlox.Structs;

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
    public readonly Instance EffectProvider;
    public readonly ParticleState[] AllParticles;

    public ParticleStream(Instance effectProvider, int maxParticles)
    {
        EffectProvider = effectProvider;
        ParticleLimit = maxParticles;
        AllParticles = new ParticleState[maxParticles];
    }

    public void StepAndRenderAllParticles()
    {
        int maxParticles = AllParticles.Length;
        if (ParticleLimit < maxParticles)
            maxParticles = ParticleLimit;

        // TODO: this
    }
}