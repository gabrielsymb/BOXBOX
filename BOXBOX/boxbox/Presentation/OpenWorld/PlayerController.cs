using Godot;

namespace Boxbox.Presentation.OpenWorld;

public sealed partial class PlayerController : CharacterBody3D
{
    private const float PlayerHeight = 1.68f;
    private const float CollisionHeight = 1.59f;
    private const float WalkSpeed = 3.2f;
    private const float SprintSpeed = 5.2f;
    private const float Gravity = 18f;
    private const float CurbStepHeight = 0.19f;
    private const float FireIntervalSeconds = 0.22f;

    private CollisionShape3D? _collision;
    private Node3D? _model;
    private Node3D? _weapon;
    private Node3D? _weaponBack;
    private AnimationPlayer? _animations;
    private DriveableCar? _vehicle;
    private double _fireCooldown;
    private double _shotPoseRemaining;
    private bool _hasWeapon;
    private bool _armed;
    private Decal? _contactShadow;
    private Node3D? _aimMarker;
    private Vector3 _aimPoint;
    private bool _hasAimPoint;

    public Vector3 FocusPosition => _vehicle?.GlobalPosition ?? Interior?.Door ?? GlobalPosition;
    public Vector3 FocusHeading => _vehicle is { } vehicle ? -vehicle.GlobalBasis.Z : GlobalBasis.Z;
    public bool IsDriving => _vehicle is not null;
    public bool ShopMenuOpen { get; set; }
    public bool IsArmed => _armed;
    public bool HasWeapon => _hasWeapon;
    internal StreetStore? Interior { get; private set; }

    internal void SetInterior(StreetStore? store)
    {
        Interior = store;
        if (store is null) return;
        _armed = false;
        _hasAimPoint = false;
        _shotPoseRemaining = 0;
        if (_weapon is not null) _weapon.Hide();
        if (_weaponBack is not null) _weaponBack.Hide();
        if (_aimMarker is not null) _aimMarker.Hide();
    }

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
            Position = new Vector3(0, CollisionHeight / 2f, 0),
            Shape = new CapsuleShape3D { Radius = 0.24f, Height = CollisionHeight }
        };
        AddChild(_collision);

        var scene = GD.Load<PackedScene>("res://Assets/Characters/NewCast/boxbox-player-fiel.glb");
        _model = scene.Instantiate<Node3D>();
        _model.Name = "ProtagonistVisual";
        _model.RotateY(-Mathf.Pi / 2f);
        VisualShadowSupport.NormalizeHeight(_model, PlayerHeight);
        VisualShadowSupport.AlignLowestPointToGround(_model, 0.02f);
        AddChild(_model);
        InteriorRenderLayer.Include(_model);
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
        if (_animations is not null)
        {
            foreach (var clip in new[] { "idle", "walk", "run" })
            {
                if (_animations.HasAnimation(clip))
                    _animations.GetAnimation(clip).LoopMode = Animation.LoopModeEnum.Linear;
            }
        }
        var skeleton = FindDescendant<Skeleton3D>(_model);
        if (skeleton is not null)
        {
            var rifleScene = GD.Load<PackedScene>("res://Assets/Characters/NewCast/Props/boxbox-rifle-fiel.glb");
            var rightHand = FindBoneEndingWith(skeleton, "RightHand");
            var spine = FindBoneEndingWith(skeleton, "Spine2") ?? FindBoneEndingWith(skeleton, "Spine1");
            if (rifleScene is not null && rightHand is not null)
            {
                var handSocket = new BoneAttachment3D { BoneName = rightHand };
                skeleton.AddChild(handSocket);
                _weapon = rifleScene.Instantiate<Node3D>();
                _weapon.Scale = Vector3.One * 0.16f;
                handSocket.AddChild(_weapon);
            }
            if (rifleScene is not null && spine is not null)
            {
                var backSocket = new BoneAttachment3D { BoneName = spine };
                skeleton.AddChild(backSocket);
                _weaponBack = rifleScene.Instantiate<Node3D>();
                _weaponBack.Scale = Vector3.One * 0.16f;
                _weaponBack.Rotation = new Vector3(Mathf.Pi / 2f, 0.05f, 0.18f);
                _weaponBack.Position = new Vector3(0.02f, -0.03f, -0.13f);
                backSocket.AddChild(_weaponBack);
            }
        }
        if (_weapon is not null) _weapon.Hide();
        if (_weaponBack is not null) _weaponBack.Hide();

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

        if (ShopMenuOpen)
        {
            Velocity = new Vector3(0, IsOnFloor() ? 0 : Velocity.Y - Gravity * (float)delta, 0);
            MoveAndSlide();
            return;
        }

        if (Interior is null && Input.IsActionJustPressed("interact"))
        {
            TryEnterNearestVehicle();
        }

        if (Interior is null && Input.IsActionJustPressed("equip_weapon"))
        {
            if (_hasWeapon)
            {
                _armed = !_armed;
                UpdateWeaponVisibility(running: false);
                if (_aimMarker is not null) _aimMarker.Visible = _armed;
            }
        }

        _hasAimPoint = _armed && UpdateAimPoint();

        if (Interior is null && _armed && _hasAimPoint && Input.IsActionPressed("fire") && _fireCooldown <= 0)
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
        var running = _armed && Input.IsActionPressed("sprint") && direction.LengthSquared() > 0.001f;
        UpdateWeaponVisibility(running);
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
                PlayAnimation(Input.IsActionPressed("sprint") ? "run" : "walk");
            }
        }
        else if (_shotPoseRemaining <= 0)
        {
            PlayAnimation(_armed ? "holding-both" : "idle");
        }

        if (Interior is { } store)
        {
            var step = (float)delta;
            var target = store.ClampInteriorPosition(GlobalPosition + Velocity * step);
            Velocity = (target - GlobalPosition) / step;
        }
        else
        {
            TryStepUp(direction * speed * (float)delta);
        }
        MoveAndSlide();
    }

    private void TryStepUp(Vector3 travel)
    {
        if (!IsOnFloor() || travel.LengthSquared() < 0.00001f)
        {
            return;
        }

        var wall = new KinematicCollision3D();
        if (!TestMove(GlobalTransform, travel, wall) ||
            Mathf.Abs(wall.GetNormal().Y) >= 0.95f ||
            wall.GetCollider() is not StaticBody3D { Name: var obstacle } ||
            (obstacle != "ExactLowLotCollision" && obstacle != RoadSurfaceCollision.CurbName &&
             obstacle != RoadSurfaceCollision.AsphaltName))
        {
            return;
        }

        var lift = Vector3.Up * CurbStepHeight;
        if (!TestMove(GlobalTransform, lift) && !TestMove(GlobalTransform.Translated(lift), travel))
        {
            GlobalPosition += lift;
        }
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

    private static string? FindBoneEndingWith(Skeleton3D skeleton, string suffix)
    {
        return Enumerable.Range(0, skeleton.GetBoneCount())
            .Select(index => skeleton.GetBoneName(index).ToString())
            .FirstOrDefault(name => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
    }

    public void AcquireWeapon()
    {
        _hasWeapon = true;
        _armed = false;
        UpdateWeaponVisibility(running: false);
    }

    private void UpdateWeaponVisibility(bool running)
    {
        var showHand = _hasWeapon && _armed && !running;
        var showBack = _hasWeapon && (!_armed || running);
        if (_weapon is not null) _weapon.Visible = showHand;
        if (_weaponBack is not null) _weaponBack.Visible = showBack;
    }

    private void PlayAnimation(string token, bool restart = false)
    {
        if (_animations is null)
        {
            return;
        }

        var clips = _animations.GetAnimationList();
        var clip = Array.Find(clips, name => name.Equals(token, StringComparison.OrdinalIgnoreCase));
        if (clip is null && token.StartsWith("holding-both", StringComparison.Ordinal))
        {
            clip = Array.Find(clips, name => name.Equals("idle", StringComparison.OrdinalIgnoreCase));
            restart = false;
        }
        if (clip is not null && (restart || _animations.CurrentAnimation != clip))
            _animations.Play(clip);
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
}
