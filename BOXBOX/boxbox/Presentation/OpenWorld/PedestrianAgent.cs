using Godot;

namespace Boxbox.Presentation.OpenWorld;

public enum PedestrianState { Walking, WaitingCrosswalk, DoingActivity, Crossing, Startled, Cowering, Fleeing }

public sealed partial class PedestrianAgent : CharacterBody3D
{
    private const float Gravity = 18f;
    private readonly List<SidewalkWaypoint> _route = [];
    private PlayerController? _player;
    private AnimationPlayer? _animations;
    private IntersectionTrafficController? _crossingController;
    private TrafficAxis _crossingAxis;
    private uint _seed;
    private uint _decisionIndex;
    private int _routeIndex;
    private float _walkSpeed;
    private float _bodyHeight;
    private double _activityRemaining;
    private double _fleeRemaining;
    private double _startledRemaining;
    private double _cowerRemaining;
    private bool _fleeAfterCower;
    private Vector3 _dangerSource;

    public PedestrianState State { get; private set; } = PedestrianState.Walking;

    public void Initialize(PlayerController player, string visualPath, uint seed)
    {
        _player = player;
        _seed = seed;
        _walkSpeed = 1.22f + Hash01(seed, 7) * 0.52f;
        var visual = GD.Load<PackedScene>(visualPath).Instantiate<Node3D>();
        visual.Name = "PedestrianVisual";
        var child = visualPath.Contains("Schoolkid", StringComparison.OrdinalIgnoreCase);
        var targetHeight = child ? .62f : .74f + Hash01(seed, 11) * .08f;
        _bodyHeight = targetHeight;
        // These animated NPC GLBs contain a 0.01 armature conversion transform.
        // Godot's mesh AABB therefore cannot be used to scale their skeleton root.
        // Source heights were batch-measured from the actual GLBs in Blender.
        visual.Scale = Vector3.One * (targetHeight / SourceHeight(visualPath));
        AddChild(visual);
        VisualShadowSupport.SetCastShadows(visual);
        _animations = FindAnimationPlayer(visual);
        AddChild(VisualShadowSupport.CreateContactShadow("PedestrianContactShadow", new Vector2(.62f, .46f)));
    }

    public override void _Ready()
    {
        AddToGroup("pedestrian");
        // Pedestrians are solid to the player/world but use local separation
        // instead of blocking one another into permanent corner clusters.
        CollisionLayer = 1u << 1;
        CollisionMask = 1u;
        FloorMaxAngle = Mathf.DegToRad(50f);
        FloorSnapLength = 0.2f;
        AddChild(new CollisionShape3D
        {
            Position = new Vector3(0, _bodyHeight * .48f, 0),
            Shape = new CapsuleShape3D
            {
                Radius = _bodyHeight * .2f,
                Height = _bodyHeight * .96f
            }
        });
        _route.AddRange(SidewalkGraph.BuildRoute(GlobalPosition, _seed));
        _routeIndex = FindNearestWaypoint();
        PlayAnimation("walk");
    }

    public override void _ExitTree() => FinishCrossing();

    public override void _PhysicsProcess(double delta)
    {
        if (_player is null || _route.Count == 0) return;
        DetectDanger();
        Vector3 desired;
        float speed;
        if (_startledRemaining > 0)
        {
            _startledRemaining -= delta;
            State = PedestrianState.Startled;
            desired = Vector3.Zero;
            speed = 0;
            FacePoint(_dangerSource);
            PlayAnimation("emote-no", "idle");
        }
        else if (_cowerRemaining > 0)
        {
            _cowerRemaining -= delta;
            State = PedestrianState.Cowering;
            desired = Vector3.Zero;
            speed = 0;
            PlayAnimation("sit", "idle");
            if (_cowerRemaining <= 0 && _fleeAfterCower) _fleeRemaining = 2.2 + Hash01(_seed, 61) * 1.8;
        }
        else if (_fleeRemaining > 0)
        {
            _fleeRemaining -= delta;
            State = PedestrianState.Fleeing;
            desired = FlatDirection(_dangerSource, GlobalPosition);
            speed = 3.15f + Hash01(_seed, 17) * .65f;
            PlayAnimation("sprint", "run", "walk");
        }
        else if (_activityRemaining > 0)
        {
            _activityRemaining -= delta;
            State = PedestrianState.DoingActivity;
            desired = Vector3.Zero;
            speed = 0;
            PlayAnimation("sit", "idle");
        }
        else (desired, speed) = FollowRoute();

        if (desired.LengthSquared() > 0.001f)
        {
            desired = (desired + SeparationSteering() * .58f).Normalized();
            // The Mini Characters rigs face +Z, opposite Godot's conventional -Z.
            Rotation = new Vector3(0, Mathf.Atan2(-desired.X, -desired.Z) + Mathf.Pi, 0);
        }
        var vertical = IsOnFloor() ? 0f : Velocity.Y - Gravity * (float)delta;
        Velocity = new Vector3(desired.X * speed, vertical, desired.Z * speed);
        MoveAndSlide();
    }

    public void NotifyDanger(Vector3 source, float seconds = 4.5f)
    {
        _dangerSource = source;
        _fleeRemaining = Math.Max(_fleeRemaining, seconds);
        _activityRemaining = 0;
        FinishCrossing();
    }

    public void NotifyGunshot(Vector3 source)
    {
        _dangerSource = source;
        _activityRemaining = 0;
        FinishCrossing();
        var roll = Hash01(_seed, 5000 + _decisionIndex++);
        if (roll < .45f)
        {
            _fleeRemaining = 3.2 + Hash01(_seed, 5100 + _decisionIndex) * 2.4;
            return;
        }
        if (roll < .76f)
        {
            _cowerRemaining = 2.2 + Hash01(_seed, 5200 + _decisionIndex) * 3.2;
            _fleeAfterCower = Hash01(_seed, 5300 + _decisionIndex) < .55f;
            return;
        }
        _startledRemaining = .7 + Hash01(_seed, 5400 + _decisionIndex) * 1.5;
    }

    private (Vector3 Direction, float Speed) FollowRoute()
    {
        var target = _route[_routeIndex];
        var toTarget = target.Position - GlobalPosition;
        toTarget.Y = 0;
        if (toTarget.Length() < .48f)
        {
            if (State == PedestrianState.Crossing) FinishCrossing();
            if (target.Kind == SidewalkWaypointKind.Activity && ShouldDoActivity())
            {
                State = PedestrianState.DoingActivity;
                _activityRemaining = 4 + Hash01(_seed, 100 + _decisionIndex) * 6;
                _decisionIndex++;
                return (Vector3.Zero, 0);
            }
            _routeIndex = (_routeIndex + 1) % _route.Count;
            target = _route[_routeIndex];
            toTarget = target.Position - GlobalPosition;
            toTarget.Y = 0;
        }

        if (target.Kind == SidewalkWaypointKind.Crosswalk && State != PedestrianState.Crossing)
        {
            var controller = IntersectionTrafficController.FindNearestCrossing(GetTree(), GlobalPosition.Lerp(target.Position, .5f));
            if (controller is not null && !controller.CanPedestrianStart(target.CrossedVehicleAxis))
            {
                State = PedestrianState.WaitingCrosswalk;
                PlayAnimation("idle");
                return (Vector3.Zero, 0);
            }
            _crossingController = controller;
            _crossingAxis = target.CrossedVehicleAxis;
            controller?.BeginPedestrianCrossing(_crossingAxis);
            State = PedestrianState.Crossing;
        }
        else if (State != PedestrianState.Crossing) State = PedestrianState.Walking;

        PlayAnimation("walk");
        var hurry = State == PedestrianState.Crossing &&
            _crossingController?.StateForAxis(_crossingAxis) != TrafficLightState.Red ? 1.45f : 1f;
        return (toTarget.Normalized(), _walkSpeed * hurry);
    }

    private void DetectDanger()
    {
        DetectFastVehicles("traffic_vehicle");
        DetectFastVehicles("driveable_car");
    }

    private void DetectFastVehicles(string group)
    {
        foreach (var node in GetTree().GetNodesInGroup(group))
        {
            if (node is not CharacterBody3D vehicle || vehicle.Velocity.Length() < 3.8f) continue;
            if (GlobalPosition.DistanceTo(vehicle.GlobalPosition) < 2.2f) NotifyDanger(vehicle.GlobalPosition, 2.8f);
        }
    }

    private Vector3 SeparationSteering()
    {
        var separation = Vector3.Zero;
        foreach (var node in GetTree().GetNodesInGroup("pedestrian"))
        {
            if (node is not PedestrianAgent other || other == this) continue;
            var away = GlobalPosition - other.GlobalPosition;
            away.Y = 0;
            var distance = away.Length();
            if (distance is <= .01f or >= 1.15f) continue;
            separation += away.Normalized() * (1f - distance / 1.15f);
        }
        return separation;
    }

    private bool ShouldDoActivity() => Hash01(_seed, 1000 + _decisionIndex) < .30f;

    private int FindNearestWaypoint()
    {
        var nearest = 0;
        var distance = float.MaxValue;
        for (var index = 0; index < _route.Count; index++)
        {
            var candidate = GlobalPosition.DistanceSquaredTo(_route[index].Position);
            if (candidate >= distance) continue;
            nearest = index;
            distance = candidate;
        }
        return nearest;
    }

    private void FinishCrossing()
    {
        if (_crossingController is null) return;
        if (IsInstanceValid(_crossingController)) _crossingController.EndPedestrianCrossing(_crossingAxis);
        _crossingController = null;
    }

    private void PlayAnimation(params string[] tokens)
    {
        if (_animations is null) return;
        foreach (var token in tokens)
        foreach (var animation in _animations.GetAnimationList())
        {
            var name = animation.ToString();
            if (!name.Contains(token, StringComparison.OrdinalIgnoreCase)) continue;
            if (_animations.CurrentAnimation != name) _animations.Play(animation);
            return;
        }
    }

    private static AnimationPlayer? FindAnimationPlayer(Node node)
    {
        if (node is AnimationPlayer player) return player;
        foreach (var child in node.GetChildren())
        {
            var found = FindAnimationPlayer(child);
            if (found is not null) return found;
        }
        return null;
    }

    private static Vector3 FlatDirection(Vector3 from, Vector3 to)
    {
        var result = to - from;
        result.Y = 0;
        return result.LengthSquared() > .001f ? result.Normalized() : Vector3.Forward;
    }

    private void FacePoint(Vector3 point)
    {
        var direction = point - GlobalPosition;
        direction.Y = 0;
        if (direction.LengthSquared() <= .001f) return;
        Rotation = new Vector3(0, Mathf.Atan2(-direction.X, -direction.Z) + Mathf.Pi, 0);
    }

    private static float Hash01(uint seed, uint salt)
    {
        var value = seed ^ (salt * 0x9E3779B9u);
        value ^= value >> 16;
        value *= 0x7FEB352Du;
        value ^= value >> 15;
        return (value & 0x00FFFFFFu) / 16777215f;
    }

    private static float SourceHeight(string path)
    {
        if (path.Contains("Cashier", StringComparison.OrdinalIgnoreCase)) return .145569f;
        if (path.Contains("FoodCourier", StringComparison.OrdinalIgnoreCase)) return .150367f;
        if (path.Contains("GrandmaShopper", StringComparison.OrdinalIgnoreCase)) return .150265f;
        if (path.Contains("OfficeLady", StringComparison.OrdinalIgnoreCase)) return .150366f;
        if (path.Contains("Schoolkid", StringComparison.OrdinalIgnoreCase)) return .132623f;
        if (path.Contains("Shoplifter", StringComparison.OrdinalIgnoreCase)) return .151407f;
        if (path.Contains("StockClerk", StringComparison.OrdinalIgnoreCase)) return .132901f;
        if (path.Contains("StoreManager", StringComparison.OrdinalIgnoreCase)) return .140219f;
        return .15f;
    }
}
