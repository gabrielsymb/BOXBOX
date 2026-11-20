using Godot;

namespace Boxbox.Presentation.OpenWorld;

public sealed partial class NightWindowController : Node
{
    public uint BuildingSeed { get; set; }
    private readonly List<StandardMaterial3D> _materials = [];
    private BoxboxDayNightCycle? _cycle;
    public override void _Ready()
    {
        Collect(GetParent());
        if (GetTree().GetFirstNodeInGroup("day_night_cycle") is not BoxboxDayNightCycle cycle) return;
        _cycle = cycle;
        cycle.DayStarted += OnDayStarted;
        cycle.NightStarted += OnNightStarted;
        SetLit(cycle.IsNight());
    }
    public override void _ExitTree()
    {
        if (_cycle is null) return;
        _cycle.DayStarted -= OnDayStarted;
        _cycle.NightStarted -= OnNightStarted;
        _cycle = null;
    }
    private void OnDayStarted() => SetLit(false);
    private void OnNightStarted() => SetLit(true);
    private void Collect(Node node)
    {
        if (node is MeshInstance3D mesh)
        for (var i = 0; i < mesh.Mesh.GetSurfaceCount(); i++)
        {
            var source = mesh.GetActiveMaterial(i);
            if (source is not StandardMaterial3D material ||
                !string.Equals(material.ResourceName, "Window_Glass", StringComparison.OrdinalIgnoreCase))
                continue;

            // Material-only controller: window geometry must come from real faces
            // authored in Blender. Never synthesize meshes or overlays here.
            var copy = (StandardMaterial3D)material.Duplicate();
            mesh.SetSurfaceOverrideMaterial(i, copy);
            _materials.Add(copy);
        }
        foreach (var child in node.GetChildren()) Collect(child);
    }
    private void SetLit(bool lit)
    {
        foreach (var material in _materials)
        {
            material.EmissionEnabled = lit;
            material.Emission = Color.FromHtml("ffbe55");
            material.EmissionEnergyMultiplier = 1.8f;
        }
    }
}
