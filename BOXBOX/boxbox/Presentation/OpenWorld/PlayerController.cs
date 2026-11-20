using Godot;

namespace Boxbox.Presentation.OpenWorld;

public sealed partial class PlayerController : CharacterBody3D
{
    private const float WalkSpeed = 3.2f;
    private const float SprintSpeed = 5.2f;
    private const float Gravity = 18f;
    private const float FireIntervalSeconds = 0.22f;

    private CollisionShape3D? _collision;
    private Node3D? _model;
    private Node3D? _weapon;
    private AnimationPlayer? _animations;
    private DriveableCar? _vehicle;
    private double _fireCooldown;
    private double _shotPoseRemaining;
    private bool _armed;
    private Decal? _contactShadow;
    private Node3D? _aimMarker;
    private Vector3 _aimPoint;
    private bool _hasAimPoint;

    public Vector3 FocusPosition => _vehicle?.GlobalPosition ?? GlobalPosition;
    public bool IsArmed => _armed;

    public override void _Ready()
    {
        Name = "BOXBOX_Protagonist";
        AddToGroup("player");
        CollisionLayer = 1u;
        CollisionMask = 1u | (1u << 1);
        FloorMaxAngle = Mathf.DegToRad(50f);
        FloorSnapLength = 0.22f;
        FloorStopOnSlope = true;

        _collision = new CollisionShape3D
        {
            Name = "PlayerCollision",
            Position = new Vector3(0, 0.37f, 0),
            Shape = new CapsuleShape3D { Radius = 0.17f, Height = 0.74f }
        };
        AddChild(_collision);

        var scene = GD.Load<PackedScene>("res://Assets/Characters/boxbox-protagonist.glb");
        _model = scene.Instantiate<Node3D>();
        _model.Name = "ApprovedProtagonistVisual";
        VisualShadowSupport.NormalizeHeight(_model, 0.78f);
        VisualShadowSupport.AlignLowestPointToGround(_model, 0.02f);
        AddChild(_model);
        VisualShadowSupport.SetCastShadows(_model);
        _contactShadow = VisualShadowSupport.CreateContactShadow("PlayerContactShadow", new Vector2(0.72f, 0.52f));
        AddChild(_contactShadow);

        _aimMarker = GD.Load<PackedScene>("res://Assets/World/Props/target.glb").Instantiate<Node3D>();
        _aimMarker.Name = "KenneyAimMarker";
        _aimMarker.Scale = Vector3.One * .42f;
        _aimMarker.TopLevel = true;
        _aimMarker.Visible = false;
        AddChild(_aimMarker);
        VisualShadowSupport.SetCastShadows(_aimMarker, false);

        _animations = FindDescendant<AnimationPlayer>(_model);
        _weapon = FindDescendantByName(_model, "BOXBOX_WeaponShooter");
        if (_weapon is not null)
        {
            _weapon.Visible = false;
        }

        PlayAnimation("idle");
    }

    public override void _PhysicsProcess(double delta)
    {
        _fireCooldown = Mathf.Max(0, _fireCooldown - delta);
        _shotPoseRemaining = Mathf.Max(0, _shotPoseRemaining - delta);

        if (_vehicle is not null)
        {
            GlobalPosition = _vehicle.GlobalPosition;
            if (Input.IsActionJustPressed("interact"))
            {
                ExitVehicle();
            }

            return;
        }

        if (Input.IsActionJustPressed("interact"))
        {
            TryEnterNearestVehicle();
        }

        if (Input.IsActionJustPressed("equip_weapon"))
        {
            _armed = !_armed;
            if (_weapon is not null)
            {
                _weapon.Visible = _armed;
            }
            if (_aimMarker is not null) _aimMarker.Visible = _armed;
        }

        _hasAimPoint = _armed && UpdateAimPoint();

        if (_armed && _hasAimPoint && Input.IsActionPressed("fire") && _fireCooldown <= 0)
        {
            Fire();
        }

        var input = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        var direction = new Vector3(input.X, 0, input.Y);
        if (direction.LengthSquared() > 1)
        {
            direction = direction.Normalized();
        }

        var speed = Input.IsActionPressed("sprint") ? SprintSpeed : WalkSpeed;
        Velocity = new Vector3(direction.X * speed, Velocity.Y, direction.Z * speed);
        if (!IsOnFloor())
        {
            Velocity = new Vector3(Velocity.X, Velocity.Y - Gravity * (float)delta, Velocity.Z);
        }

        if (direction.LengthSquared() > 0.001f)
        {
            // The approved Kenney rig faces +Z while Godot's conventional forward is -Z.
            if (!_armed) Rotation = new Vector3(0, Mathf.Atan2(-direction.X, -direction.Z) + Mathf.Pi, 0);
            if (_shotPoseRemaining <= 0)
            {
                PlayAnimation(Input.IsActionPressed("sprint") ? "sprint" : "walk");
            }
        }
        else if (_shotPoseRemaining <= 0)
        {
            PlayAnimation(_armed ? "holding-both" : "idle");
        }

        MoveAndSlide();
    }

    private void Fire()
    {
        _fireCooldown = FireIntervalSeconds;
        _shotPoseRemaining = 0.18;
        PlayAnimation("holding-both-shoot", restart: true);
        foreach (var node in GetTree().GetNodesInGroup("pedestrian"))
            if (node is PedestrianAgent pedestrian && GlobalPosition.DistanceTo(pedestrian.GlobalPosition) < 13f)
                pedestrian.NotifyGunshot(GlobalPosition);

        var flatAim = _aimPoint - GlobalPosition;
        flatAim.Y = 0;
        if (flatAim.LengthSquared() <= .001f) return;
        var forward = flatAim.Normalized();
        var origin = GlobalPosition + Vector3.Up * .43f + forward * .22f;
        var targetPoint = new Vector3(_aimPoint.X, origin.Y, _aimPoint.Z);
        var shotDirection = (targetPoint - origin).Normalized();
        var query = PhysicsRayQueryParameters3D.Create(origin, origin + shotDirection * 35f);
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
        var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
        if (hit.Count == 0)
        {
            return;
        }

        var collider = hit["collider"].AsGodotObject();
        if (collider is DamageableTarget target)
        {
            target.ApplyHit(1);
        }
    }

    private bool UpdateAimPoint()
    {
        var camera = GetViewport().GetCamera3D();
        if (camera is null) return false;
        var mouse = GetViewport().GetMousePosition();
        var from = camera.ProjectRayOrigin(mouse);
        var to = from + camera.ProjectRayNormal(mouse) * 200f;
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
        var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
        if (hit.Count == 0) return false;
        _aimPoint = hit["position"].AsVector3();
        var facing = _aimPoint - GlobalPosition;
        facing.Y = 0;
        if (facing.LengthSquared() <= .001f) return false;
        Rotation = new Vector3(0, Mathf.Atan2(-facing.X, -facing.Z) + Mathf.Pi, 0);
        if (_aimMarker is not null)
        {
            _aimMarker.Visible = true;
            _aimMarker.GlobalPosition = _aimPoint + Vector3.Up * .04f;
            var towardCamera = camera.GlobalPosition - _aimPoint;
            _aimMarker.Rotation = new Vector3(0, Mathf.Atan2(towardCamera.X, towardCamera.Z), 0);
        }
        return true;
    }

    private void TryEnterNearestVehicle()
    {
        DriveableCar? nearest = null;
        var nearestDistance = 2.6f;
        foreach (var node in GetTree().GetNodesInGroup("driveable_car"))
        {
            if (node is not DriveableCar car || car.HasDriver)
            {
                continue;
            }

            var distance = GlobalPosition.DistanceTo(car.GlobalPosition);
            if (distance < nearestDistance)
            {
                nearest = car;
                nearestDistance = distance;
            }
        }

        nearest?.Enter(this);
    }

    public void BeginDriving(DriveableCar vehicle)
    {
        _vehicle = vehicle;
        Velocity = Vector3.Zero;
        if (_model is not null)
        {
            _model.Visible = false;
        }
        if (_contactShadow is not null) _contactShadow.Visible = false;
        if (_aimMarker is not null) _aimMarker.Visible = false;

        if (_collision is not null)
        {
            _collision.SetDeferred(CollisionShape3D.PropertyName.Disabled, true);
        }
    }

    private void ExitVehicle()
    {
        if (_vehicle is null)
        {
            return;
        }

        var vehicle = _vehicle;
        _vehicle = null;
        vehicle.Exit(this);
        GlobalPosition = vehicle.GlobalPosition + vehicle.GlobalTransform.Basis.X.Normalized() * 2.1f + Vector3.Up * 0.2f;
        if (_model is not null)
        {
            _model.Visible = true;
        }
        if (_contactShadow is not null) _contactShadow.Visible = true;
        if (_aimMarker is not null) _aimMarker.Visible = _armed;

        if (_collision is not null)
        {
            _collision.SetDeferred(CollisionShape3D.PropertyName.Disabled, false);
        }
    }

    private void PlayAnimation(string token, bool restart = false)
    {
        if (_animations is null)
        {
            return;
        }

        foreach (var candidate in _animations.GetAnimationList())
        {
            var name = candidate.ToString();
            if (!name.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!restart && _animations.CurrentAnimation == name)
            {
                return;
            }

            _animations.Play(candidate);
            return;
        }
    }

    private static T? FindDescendant<T>(Node root) where T : Node
    {
        if (root is T typed)
        {
            return typed;
        }

        foreach (var child in root.GetChildren())
        {
            var result = FindDescendant<T>(child);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    private static Node3D? FindDescendantByName(Node root, string name)
    {
        if (root is Node3D node3D && root.Name.ToString().Contains(name, StringComparison.OrdinalIgnoreCase))
        {
            return node3D;
        }

        foreach (var child in root.GetChildren())
        {
            var result = FindDescendantByName(child, name);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }
}
