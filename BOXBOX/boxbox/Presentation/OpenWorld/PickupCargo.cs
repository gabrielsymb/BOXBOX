using Godot;

namespace Boxbox.Presentation.OpenWorld;

/// <summary>Bounded inertial motion of the boxes which represent a confirmed load.</summary>
internal sealed partial class PickupCargo : Node3D
{
    private static readonly Vector3[] Slots =
    [
        new(-0.27f, 0.369f, 0.76f),
        new(0.27f, 0.369f, 0.76f),
        new(-0.27f, 0.369f, 1.27f),
        new(0.27f, 0.369f, 1.27f)
    ];

    private sealed class LoadBox(Node3D visual)
    {
        public readonly Node3D Visual = visual;
        public Vector2 Offset;
        public Vector2 Velocity;
        public Vector2 Tilt;
    }

    private readonly List<LoadBox> _boxes = [];
    private PackedScene? _boxModel;

    public void ShowUnits(int units)
    {
        var count = Math.Min(Slots.Length, (units + 5) / 6);
        while (_boxes.Count > count)
        {
            var box = _boxes[^1].Visual;
            _boxes.RemoveAt(_boxes.Count - 1);
            RemoveChild(box);
            box.QueueFree();
        }

        while (_boxes.Count < count)
        {
            _boxModel ??= GD.Load<PackedScene>("res://Assets/World/Cargo/stock-box.glb");
            var box = _boxModel.Instantiate<Node3D>();
            box.Scale = new Vector3(0.68f, 0.73f, 0.64f);
            box.Position = Slots[_boxes.Count];
            VisualShadowSupport.SetCastShadows(box);
            AddChild(box);
            _boxes.Add(new LoadBox(box));
        }
    }

    public void Step(Vector3 localAcceleration, float delta)
    {
        var push = -new Vector2(localAcceleration.X, localAcceleration.Z).LimitLength(28f);
        for (var index = 0; index < _boxes.Count; index++)
        {
            var box = _boxes[index];
            var response = 0.38f - index * 0.035f;
            box.Velocity += (push * response - box.Offset * 26f - box.Velocity * 6.6f) * delta;
            box.Offset += box.Velocity * delta;
            var limited = new Vector2(Mathf.Clamp(box.Offset.X, -0.11f, 0.11f),
                Mathf.Clamp(box.Offset.Y, -0.075f, 0.075f));
            if (limited.X != box.Offset.X) box.Velocity.X *= -0.25f;
            if (limited.Y != box.Offset.Y) box.Velocity.Y *= -0.25f;
            box.Offset = limited;

            // The higher edge rocks over the lower edge, close to the bed floor.
            box.Tilt = box.Tilt.Lerp(box.Offset * 1.33f, 1f - Mathf.Exp(-13f * delta));
            var lift = Mathf.Abs(box.Tilt.X) * 0.24f + Mathf.Abs(box.Tilt.Y) * 0.23f;
            box.Visual.Position = Slots[index] + new Vector3(limited.X, lift, limited.Y);
            box.Visual.Rotation = new Vector3(-box.Tilt.Y, index % 2 == 0 ? 0.025f : -0.025f, box.Tilt.X);
        }
    }
}
