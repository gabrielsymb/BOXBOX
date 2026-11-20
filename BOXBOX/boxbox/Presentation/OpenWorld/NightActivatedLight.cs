using Godot;

namespace Boxbox.Presentation.OpenWorld;

/// <summary>Presentation-only warm light synchronized with the visual day/night rig.</summary>
public sealed partial class NightActivatedLight : OmniLight3D
{
    private double _transitionDelay;
    private bool _requestedState;
    private readonly List<StandardMaterial3D> _bulbMaterials = [];
    private BoxboxDayNightCycle? _cycle;

    public override void _Ready()
    {
        LightColor = Color.FromHtml("fff1c2");
        LightEnergy = 2.1f;
        OmniRange = 8.0f;
        ShadowEnabled = false;
        Visible = false;
        CollectBulbMaterials(GetParent());

        if (GetTree().GetFirstNodeInGroup("day_night_cycle") is not BoxboxDayNightCycle cycle) return;
        _cycle = cycle;
        cycle.DayStarted += OnDayStarted;
        cycle.NightStarted += OnNightStarted;
        Schedule(cycle.IsNight());
    }

    public override void _ExitTree()
    {
        if (_cycle is null) return;
        _cycle.DayStarted -= OnDayStarted;
        _cycle.NightStarted -= OnNightStarted;
        _cycle = null;
    }

    private void OnDayStarted() => Schedule(false);
    private void OnNightStarted() => Schedule(true);

    public override void _Process(double delta)
    {
        if (_transitionDelay <= 0) return;
        _transitionDelay -= delta;
        if (_transitionDelay <= 0)
        {
            Visible = _requestedState;
            foreach (var material in _bulbMaterials)
            {
                material.EmissionEnabled = _requestedState;
                material.Emission = Color.FromHtml("ffd27d");
                material.EmissionEnergyMultiplier = 3f;
            }
        }
    }

    private void Schedule(bool enabled)
    {
        _requestedState = enabled;
        var stableOffset = Math.Abs(GlobalPosition.X * 17f + GlobalPosition.Z * 31f);
        _transitionDelay = 0.08 + (stableOffset % 90f) / 100f;
    }

    private void CollectBulbMaterials(Node node)
    {
        if (node is MeshInstance3D mesh && mesh.Mesh is not null)
        {
            for (var surface = 0; surface < mesh.Mesh.GetSurfaceCount(); surface++)
            {
                if (mesh.GetActiveMaterial(surface) is not StandardMaterial3D source ||
                    source.ResourceName != "Streetlight_Bulb") continue;
                var copy = (StandardMaterial3D)source.Duplicate();
                mesh.SetSurfaceOverrideMaterial(surface, copy);
                _bulbMaterials.Add(copy);
            }
        }
        foreach (var child in node.GetChildren()) CollectBulbMaterials(child);
    }
}
