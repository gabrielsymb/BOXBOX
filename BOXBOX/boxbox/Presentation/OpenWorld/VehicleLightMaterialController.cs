using Godot;

namespace Boxbox.Presentation.OpenWorld;

/// <summary>
/// Controls only materials authored on real vehicle faces in Blender.
/// It deliberately creates no meshes, decals, or light overlays.
/// </summary>
public sealed partial class VehicleLightMaterialController : Node
{
    private readonly List<StandardMaterial3D> _headlights = [];
    private readonly List<StandardMaterial3D> _taillights = [];

    public void Bind(Node visual)
    {
        Collect(visual);
        SetHeadlights(false);
        SetTaillights(false);
    }

    public void SetHeadlights(bool enabled)
    {
        SetEmission(_headlights, enabled, Color.FromHtml("fff4cc"), 2.5f);
    }

    public void SetTaillights(bool enabled)
    {
        SetEmission(_taillights, enabled, Color.FromHtml("ff1a1a"), 3f);
    }

    private void Collect(Node node)
    {
        if (node is MeshInstance3D mesh && mesh.Mesh is not null)
        {
            for (var surface = 0; surface < mesh.Mesh.GetSurfaceCount(); surface++)
            {
                if (mesh.GetActiveMaterial(surface) is not StandardMaterial3D source)
                    continue;

                List<StandardMaterial3D>? target = source.ResourceName switch
                {
                    "Headlight_Mat" => _headlights,
                    "Taillight_Mat" => _taillights,
                    _ => null
                };
                if (target is null) continue;

                var instanceMaterial = (StandardMaterial3D)source.Duplicate();
                mesh.SetSurfaceOverrideMaterial(surface, instanceMaterial);
                target.Add(instanceMaterial);
            }
        }

        foreach (var child in node.GetChildren()) Collect(child);
    }

    private static void SetEmission(
        IEnumerable<StandardMaterial3D> materials, bool enabled, Color color, float energy)
    {
        foreach (var material in materials)
        {
            material.EmissionEnabled = enabled;
            material.Emission = color;
            material.EmissionEnergyMultiplier = energy;
        }
    }
}
