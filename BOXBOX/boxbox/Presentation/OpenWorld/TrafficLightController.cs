using Godot;

namespace Boxbox.Presentation.OpenWorld;

public enum TrafficLightState { Green, Yellow, Red }

public sealed partial class TrafficLightController : Node
{
    private readonly Dictionary<TrafficLightState, List<StandardMaterial3D>> _materials = new()
    {
        [TrafficLightState.Green] = [],
        [TrafficLightState.Yellow] = [],
        [TrafficLightState.Red] = []
    };
    private OmniLight3D? _activeLight;
    public TrafficLightState CurrentState { get; private set; } = TrafficLightState.Green;

    public void Bind(Node visual)
    {
        Collect(visual);
        var visualHeight = visual is Node3D visual3D ? VisualShadowSupport.GetVisualHeight(visual3D) : 3f;
        _activeLight = new OmniLight3D
        {
            Name = "ActiveLensLight",
            Position = new Vector3(0, MathF.Max(1.15f, visualHeight * .82f), 0),
            OmniRange = 4.4f,
            LightEnergy = 1.8f,
            DistanceFadeEnabled = true,
            DistanceFadeBegin = 16f,
            DistanceFadeLength = 6f,
            ShadowEnabled = false
        };
        GetParent().AddChild(_activeLight);
        ApplyState();
    }

    public void SetState(TrafficLightState state)
    {
        if (CurrentState == state) return;
        CurrentState = state;
        ApplyState();
    }

    private void ApplyState()
    {
        var activeColor = CurrentState switch
        {
            TrafficLightState.Red => Color.FromHtml("ff1a1a"),
            TrafficLightState.Yellow => Color.FromHtml("ffd13b"),
            _ => Color.FromHtml("39ff63")
        };
        if (_activeLight is not null)
        {
            _activeLight.LightColor = activeColor;
            _activeLight.Visible = true;
        }
        foreach (var (state, materials) in _materials)
        foreach (var material in materials)
        {
            var enabled = state == CurrentState;
            material.EmissionEnabled = enabled;
            material.EmissionEnergyMultiplier = 5f;
            material.Emission = state == CurrentState ? activeColor : Colors.Black;
        }
    }

    private void Collect(Node node)
    {
        if (node is MeshInstance3D mesh && mesh.Mesh is not null)
        for (var surface = 0; surface < mesh.Mesh.GetSurfaceCount(); surface++)
        {
            if (mesh.GetActiveMaterial(surface) is not StandardMaterial3D source) continue;
            var state = source.ResourceName switch
            {
                "TrafficLight_Red" => TrafficLightState.Red,
                "TrafficLight_Yellow" => TrafficLightState.Yellow,
                "TrafficLight_Green" => TrafficLightState.Green,
                _ => (TrafficLightState?)null
            };
            if (state is null) continue;
            var copy = (StandardMaterial3D)source.Duplicate();
            mesh.SetSurfaceOverrideMaterial(surface, copy);
            _materials[state.Value].Add(copy);
        }
        foreach (var child in node.GetChildren()) Collect(child);
    }
}
