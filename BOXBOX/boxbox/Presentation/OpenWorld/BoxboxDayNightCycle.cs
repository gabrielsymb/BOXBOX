using Godot;

namespace Boxbox.Presentation.OpenWorld;

/// <summary>One physical directional source acts as sun and moon, preserving real moving shadows.</summary>
public sealed partial class BoxboxDayNightCycle : Node3D
{
    [Signal] public delegate void DayStartedEventHandler();
    [Signal] public delegate void NightStartedEventHandler();

    [Export] public DirectionalLight3D? SunLight { get; set; }
    [Export] public WorldEnvironment? WorldEnvironment { get; set; }
    [Export(PropertyHint.Range, "30,600,1")] public float DayLengthSeconds { get; set; } = 120f;

    private float _time = 0.32f;
    private bool _wasNight;

    public override void _Ready()
    {
        AddToGroup("day_night_cycle");
        ConfigureSun();
        ApplyLighting();
        _wasNight = IsNight();
    }

    public override void _Process(double delta)
    {
        _time = Mathf.PosMod(_time + (float)delta / DayLengthSeconds, 1f);
        ApplyLighting();
        var night = IsNight();
        if (night == _wasNight) return;
        _wasNight = night;
        EmitSignal(night ? SignalName.NightStarted : SignalName.DayStarted);
    }

    public bool IsNight() => _time < 0.23f || _time >= 0.76f;
    public bool is_night() => IsNight(); // Stable callable contract for existing components.

    private void ConfigureSun()
    {
        if (SunLight is null) return;
        SunLight.ShadowEnabled = true;
        SunLight.DirectionalShadowMode = DirectionalLight3D.ShadowMode.Orthogonal;
        SunLight.DirectionalShadowMaxDistance = 45f;
        SunLight.DirectionalShadowFadeStart = 0.9f;
        SunLight.DirectionalShadowPancakeSize = 8f;
        SunLight.ShadowBias = 0.12f;
        SunLight.ShadowNormalBias = 0.85f;
        SunLight.ShadowBlur = 0f;
        SunLight.LightAngularDistance = 0f;
        RenderingServer.DirectionalShadowAtlasSetSize(4096, true);
        RenderingServer.DirectionalSoftShadowFilterSetQuality(RenderingServer.ShadowQuality.Hard);
    }

    private void ApplyLighting()
    {
        if (SunLight is null) return;
        SunLight.ShadowEnabled = true;
        var solarHeight = Mathf.Sin((_time - 0.25f) * Mathf.Tau);
        var daylight = Mathf.SmoothStep(0f, 1f, Mathf.Clamp(solarHeight, 0f, 1f));
        var night = IsNight();
        // Compatibility renderer changes shadowed-light composition. Keep the source
        // above the horizon and rotate its yaw so real shadows remain visible all day/night.
        SunLight.RotationDegrees = new Vector3(night ? -52f : Mathf.Lerp(-38f, -58f, daylight),
            night ? -128f : -52f + _time * 90f, 0);
        SunLight.LightEnergy = night ? 0.25f : Mathf.Lerp(0.62f, 0.88f, daylight);
        SunLight.LightColor = night ? Color.FromHtml("8faee8") :
            Color.FromHtml(daylight < 0.42f ? "ffad72" : "fff3d6");

        var environment = WorldEnvironment?.Environment;
        if (environment is null) return;
        environment.TonemapExposure = night ? 0.9f : 0.55f;
        // Keep the sky as the ambient source so the fill follows the current
        // sky palette instead of tinting every face with one fixed color.
        environment.AmbientLightSource = Godot.Environment.AmbientSource.Sky;
        environment.AmbientLightSkyContribution = night ? 0.68f : 0.82f;
        environment.AmbientLightEnergy = night ? 0.44f : Mathf.Lerp(0.48f, 0.60f, daylight);
    }
}
