using Godot;

namespace Boxbox.Presentation.OpenWorld;

internal static class InteriorRenderLayer
{
    public const uint Mask = 1u << 1;

    public static void Include(Node root)
    {
        if (root is VisualInstance3D visual)
            visual.Layers |= Mask;

        foreach (var child in root.GetChildren())
            Include(child);
    }
}
