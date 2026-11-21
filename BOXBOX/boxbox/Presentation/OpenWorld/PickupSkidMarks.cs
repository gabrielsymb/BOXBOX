using Godot;

namespace Boxbox.Presentation.OpenWorld;

internal sealed partial class PickupSkidMarks : Node3D
{
    private const int MaximumStrips = 160;
    private const float SegmentLength = 0.16f;
    private const float TireWidth = 0.18f;
    private const float AsphaltVisualOffset = 0.004f;
    private readonly Vector3?[] _previousContact = new Vector3?[2];
    private readonly Queue<Node3D> _strips = new();
    private PackedScene? _stripScene;

    public void UpdateTracks(DriveableCar car, bool skidding)
    {
        if (!skidding)
        {
            Array.Clear(_previousContact);
            return;
        }

        for (var index = 0; index < _previousContact.Length; index++)
        {
            var tire = car.ToGlobal(new Vector3(index == 0 ? -0.64f : 0.64f, 0, 1.06f));
            var ray = PhysicsRayQueryParameters3D.Create(tire + Vector3.Up * 0.35f,
                tire + Vector3.Down * 0.4f, 1, new Godot.Collections.Array<Rid> { car.GetRid() });
            var contact = car.GetWorld3D().DirectSpaceState.IntersectRay(ray);
            if (contact.Count == 0 || contact["collider"].AsGodotObject() is not StaticBody3D body ||
                body.Name != RoadSurfaceCollision.AsphaltName)
            {
                _previousContact[index] = null;
                continue;
            }

            var point = contact["position"].AsVector3() + Vector3.Up * AsphaltVisualOffset;
            if (_previousContact[index] is { } previous)
            {
                var distance = point.DistanceTo(previous);
                if (distance < SegmentLength) continue;
                if (distance < 0.75f) AddStrip(previous, point, distance);
            }
            _previousContact[index] = point;
        }
    }

    private void AddStrip(Vector3 from, Vector3 to, float length)
    {
        _stripScene ??= GD.Load<PackedScene>("res://Assets/World/PlayerVehicles/Skid/TireStrip.tscn");
        var strip = _stripScene.Instantiate<Node3D>();
        AddChild(strip);
        strip.GlobalPosition = (from + to) * 0.5f;
        strip.LookAt(to, Vector3.Up);
        strip.Scale = new Vector3(TireWidth, 1, length + 0.006f);
        _strips.Enqueue(strip);
        if (_strips.Count > MaximumStrips)
        {
            _strips.Dequeue().QueueFree();
        }
    }
}
