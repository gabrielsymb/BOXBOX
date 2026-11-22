using Godot;

namespace Boxbox.Presentation.OpenWorld;

/// <summary>Collision dimensions of the scaled Kenney street modules.</summary>
internal static class RoadSurfaceCollision
{
    public const string AsphaltName = "RoadAsphalt";
    public const string CurbName = "RoadCurb";
    private const float AsphaltHeight = 0.04f;
    private const float CurbHeight = 0.08f;
    private const float GroundBottom = -0.03f;
    private const float CurbWidth = 0.8f;
    private const float CurbCenter = 3.6f;

    public static void AddTo(Node3D cell, Vector3 center, bool intersection, bool vertical)
    {
        var yaw = !intersection && vertical ? Mathf.Pi / 2f : 0;
        var length = intersection ? 8f : 4f;
        var asphalt = NewBody(AsphaltName, center, yaw);
        AddBox(asphalt, Vector2.Zero, new Vector2(length, 8f), GroundBottom, AsphaltHeight);
        cell.AddChild(asphalt);

        var curb = NewBody(CurbName, center, yaw);
        if (intersection)
        {
            foreach (var x in new[] { -CurbCenter, CurbCenter })
            foreach (var z in new[] { -CurbCenter, CurbCenter })
                AddBox(curb, new Vector2(x, z), new Vector2(CurbWidth, CurbWidth), AsphaltHeight, CurbHeight);
        }
        else
        {
            AddBox(curb, new Vector2(0, -CurbCenter), new Vector2(length, CurbWidth), AsphaltHeight, CurbHeight);
            AddBox(curb, new Vector2(0, CurbCenter), new Vector2(length, CurbWidth), AsphaltHeight, CurbHeight);
        }
        cell.AddChild(curb);
    }

    private static StaticBody3D NewBody(string name, Vector3 position, float yaw) =>
        new() { Name = name, Position = position, Rotation = new Vector3(0, yaw, 0) };

    private static void AddBox(StaticBody3D body, Vector2 center, Vector2 footprint, float bottom, float top) =>
        body.AddChild(new CollisionShape3D
        {
            Position = new Vector3(center.X, (top + bottom) / 2f, center.Y),
            Shape = new BoxShape3D { Size = new Vector3(footprint.X, top - bottom, footprint.Y) }
        });
}
