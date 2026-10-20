using UnityEngine;

/// <summary>
/// PS1-style shower water. Emits from a rectangle and moves straight down
/// in WORLD space, regardless of the shower object's rotation.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class ShowerWaterEffect : MonoBehaviour
{
    [Header("Shower")]
    public bool showerOn = true;
    [Min(0.05f)] public float streamLength = 2f;
    [Min(0.1f)] public float fallSpeed = 20f;
    [Range(10f, 1000f)] public float particlesPerSecond = 250f;
    [Range(0.01f, 0.5f)] public float sprayRadius = 0.12f;

    [Header("Appearance")]
    [Range(0.005f, 0.12f)] public float dropletSize = 0.025f;
    public Color waterColor = new Color(0.65f, 0.85f, 1f, 0.65f);
    [Tooltip("Optional particle material. Leave empty to keep the material assigned in the Particle System Renderer.")]
    public Material waterMaterial;

    [Header("Force Downward Movement")]
    [Tooltip("Reapply world-space downward velocity each frame in case another component changes it.")]
    public bool forceVelocityEveryFrame = true;

    private ParticleSystem particles;
    private ParticleSystemRenderer particleRenderer;

    private void Awake()
    {
        particles = GetComponent<ParticleSystem>();
        particleRenderer = GetComponent<ParticleSystemRenderer>();
        Configure();
    }

    private void Start()
    {
        SetShower(showerOn);
    }

    private void LateUpdate()
    {
        if (forceVelocityEveryFrame && particles != null)
            ApplyDownwardVelocity();
    }

    private void OnValidate()
    {
        streamLength = Mathf.Max(0.05f, streamLength);
        fallSpeed = Mathf.Max(0.1f, fallSpeed);
        if (Application.isPlaying && particles != null)
        {
            Configure();
            SetShower(showerOn);
        }
    }

    private void Configure()
    {
        var main = particles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = streamLength / fallSpeed;
        main.startSpeed = 0f;
        main.startSize = dropletSize;
        main.startColor = waterColor;
        main.gravityModifier = 0f;
        main.maxParticles = Mathf.CeilToInt(particlesPerSecond * (streamLength / fallSpeed) * 1.5f) + 50;

        var emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = particlesPerSecond;

        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Rectangle;
        // Keep the rectangle's orientation and dimensions as set in the Inspector.
        // sprayRadius remains available for backwards compatibility with existing scenes.
        shape.randomDirectionAmount = 0f;
        shape.sphericalDirectionAmount = 0f;

        ApplyDownwardVelocity();

        var noise = particles.noise;
        noise.enabled = false;

        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        if (waterMaterial != null)
            particleRenderer.sharedMaterial = waterMaterial;
    }

    private void ApplyDownwardVelocity()
    {
        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(0f);
        velocity.y = new ParticleSystem.MinMaxCurve(-fallSpeed);
        velocity.z = new ParticleSystem.MinMaxCurve(0f);
    }

    public void TurnOn() { SetShower(true); }
    public void TurnOff() { SetShower(false); }
    public void ToggleShower() { SetShower(!showerOn); }

    public void SetShower(bool enabled)
    {
        showerOn = enabled;
        if (particles == null) particles = GetComponent<ParticleSystem>();
        if (enabled)
        {
            ApplyDownwardVelocity();
            if (!particles.isPlaying) particles.Play();
        }
        else
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
