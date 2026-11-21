using Godot;

namespace Boxbox.Presentation.OpenWorld;

/// <summary>Restores encoded vertex colors on the Polyfork character materials.</summary>
internal static class CharacterVertexPalette
{
    public static void Apply(Node visual)
    {
        foreach (var node in visual.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
        {
            if (node.Mesh is not ArrayMesh mesh) continue;
            for (var index = 0; index < mesh.GetSurfaceCount(); index++)
            {
                if ((mesh.SurfaceGetFormat(index) & Mesh.ArrayFormat.FormatColor) == 0 ||
                    node.GetActiveMaterial(index) is not StandardMaterial3D original)
                    continue;
                var colored = (StandardMaterial3D)original.Duplicate();
                colored.VertexColorUseAsAlbedo = true;
                node.SetSurfaceOverrideMaterial(index, colored);
            }
        }
    }
}
