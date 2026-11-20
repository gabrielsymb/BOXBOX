using Godot;

namespace Boxbox.Presentation.OpenWorld;

public sealed partial class DamageableTarget : StaticBody3D
{
    private const int MaximumHealth = 3;
    private const double RespawnSeconds = 4.0;

    private CollisionShape3D? _collision;
    private int _health = MaximumHealth;
    private double _respawnRemaining;

    public override void _Ready()
    {
        AddToGroup("combat_target");
        _collision = new CollisionShape3D
        {
            Position = new Vector3(0, 0.75f, 0),
            Shape = new BoxShape3D { Size = new Vector3(0.9f, 1.5f, 0.35f) }
        };
        AddChild(_collision);

        var scene = GD.Load<PackedScene>("res://Assets/World/Props/target.glb");
        var visual = scene.Instantiate<Node3D>();
        visual.Name = "ApprovedTargetVisual";
        visual.Scale = Vector3.One * 1.3f;
        AddChild(visual);
    }

    public override void _Process(double delta)
    {
        if (_respawnRemaining <= 0)
        {
            return;
        }

        _respawnRemaining -= delta;
        if (_respawnRemaining > 0)
        {
            return;
        }

        _health = MaximumHealth;
        Visible = true;
        _collision?.SetDeferred(CollisionShape3D.PropertyName.Disabled, false);
    }

    public void ApplyHit(int damage)
    {
        if (_respawnRemaining > 0 || damage <= 0)
        {
            return;
        }

        _health = Math.Max(0, _health - damage);
        if (_health > 0)
        {
            return;
        }

        Visible = false;
        _collision?.SetDeferred(CollisionShape3D.PropertyName.Disabled, true);
        _respawnRemaining = RespawnSeconds;
    }
}
