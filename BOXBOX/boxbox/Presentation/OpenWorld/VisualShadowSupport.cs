using Godot;

namespace Boxbox.Presentation.OpenWorld;

public static class VisualShadowSupport
{
    private static GradientTexture2D? _contactTexture;

    public static void SetCastShadows(Node node, bool enabled = true)
    {
        if (node is MeshInstance3D mesh)
        {
            mesh.CastShadow = enabled
                ? GeometryInstance3D.ShadowCastingSetting.On
                : GeometryInstance3D.ShadowCastingSetting.Off;
        }

        foreach (var child in node.GetChildren()) SetCastShadows(child, enabled);
    }

    public static void AlignLowestPointToGround(Node3D visual, float clearance = 0.035f)
    {
        var found = false;
        var minimumY = 0f;
        AccumulateMinimumY(visual, Transform3D.Identity, ref found, ref minimumY);
        if (!found) return;
        visual.Position += Vector3.Up * (clearance - minimumY);
    }

    public static float NormalizeHeight(Node3D visual, float targetHeight)
    {
        if (!TryGetLocalBounds(visual, out var bounds) || bounds.Size.Y <= 0.001f) return 1f;
        var factor = targetHeight / bounds.Size.Y;
        visual.Scale *= factor;
        return factor;
    }

    public static float NormalizeHorizontalLength(Node3D visual, float targetLength)
    {
        if (!TryGetLocalBounds(visual, out var bounds)) return 1f;
        var current = MathF.Max(bounds.Size.X, bounds.Size.Z);
        if (current <= 0.001f) return 1f;
        var factor = targetLength / current;
        visual.Scale *= factor;
        return factor;
    }

    public static float GetVisualHeight(Node3D visual) =>
        TryGetLocalBounds(visual, out var bounds) ? bounds.Size.Y : 0f;

    public static bool TryGetLocalBounds(Node3D visual, out Aabb bounds)
    {
        var found = false;
        var minimum = Vector3.Zero;
        var maximum = Vector3.Zero;
        AccumulateBounds(visual, Transform3D.Identity, ref found, ref minimum, ref maximum);
        bounds = found ? new Aabb(minimum, maximum - minimum) : default;
        return found;
    }

    private static void AccumulateBounds(Node3D node, Transform3D parent, ref bool found,
        ref Vector3 minimum, ref Vector3 maximum)
    {
        var transform = parent * node.Transform;
        if (node is MeshInstance3D { Mesh: not null } mesh)
        {
            var meshBounds = mesh.Mesh.GetAabb();
            for (var index = 0; index < 8; index++)
            {
                var point = transform * meshBounds.GetEndpoint(index);
                if (!found) minimum = maximum = point;
                else
                {
                    minimum = new Vector3(MathF.Min(minimum.X, point.X), MathF.Min(minimum.Y, point.Y), MathF.Min(minimum.Z, point.Z));
                    maximum = new Vector3(MathF.Max(maximum.X, point.X), MathF.Max(maximum.Y, point.Y), MathF.Max(maximum.Z, point.Z));
                }
                found = true;
            }
        }
        foreach (var child in node.GetChildren().OfType<Node3D>())
            AccumulateBounds(child, transform, ref found, ref minimum, ref maximum);
    }

    private static void AccumulateMinimumY(Node3D node, Transform3D parent,
        ref bool found, ref float minimumY)
    {
        var transform = parent * node.Transform;
        if (node is MeshInstance3D { Mesh: not null } mesh)
        {
            var bounds = mesh.Mesh.GetAabb();
            for (var index = 0; index < 8; index++)
            {
                var y = (transform * bounds.GetEndpoint(index)).Y;
                if (!found || y < minimumY) minimumY = y;
                found = true;
            }
        }
        foreach (var child in node.GetChildren().OfType<Node3D>())
            AccumulateMinimumY(child, transform, ref found, ref minimumY);
    }

    public static Decal CreateContactShadow(string name, Vector2 footprint)
    {
        _contactTexture ??= new GradientTexture2D
        {
            Width = 64,
            Height = 64,
            Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new Vector2(0.5f, 0.5f),
            FillTo = new Vector2(1f, 0.5f),
            Gradient = new Gradient
            {
                Offsets = [0f, 0.62f, 1f],
                Colors = [new Color(0, 0, 0, 0.28f), new Color(0, 0, 0, 0.16f), new Color(0, 0, 0, 0)]
            }
        };

        return new Decal
        {
            Name = name,
            Position = new Vector3(0, 0.12f, 0),
            Size = new Vector3(footprint.X, 0.32f, footprint.Y),
            TextureAlbedo = _contactTexture,
            Modulate = Colors.White,
            UpperFade = 0.12f,
            LowerFade = 0.12f,
            DistanceFadeEnabled = true,
            DistanceFadeBegin = 24f,
            DistanceFadeLength = 8f
        };
    }
}
