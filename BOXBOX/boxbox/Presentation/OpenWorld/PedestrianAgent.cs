using Godot;

namespace Boxbox.Presentation.OpenWorld;

public enum PedestrianState { Walking, WaitingCrosswalk, DoingActivity, Crossing, Startled, Cowering, Fleeing }

public sealed partial class PedestrianAgent : CharacterBody3D
{
    private const float Gravity = 18f;
    private readonly List<SidewalkWaypoint> _route = [];
    private PlayerController? _player;
    private AnimationPlayer? _animations;
    private NavigationAgent3D? _avoidance;
    private Vector3 _safeAvoidanceVelocity;
    private bool _hasAvoidanceVelocity;
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

    [Export] public bool UseReciprocalAvoidance { get; set; } = true;
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

        // Godot's built-in RVO2 solver supplies ORCA-style reciprocal
        // collision avoidance. The authored sidewalk graph is still the
        // route authority: no NavigationMesh or cloud model is required.
        // These are movement radii, NOT asset or character scale changes.
        _avoidance = new NavigationAgent3D
        {
            Name = "PedestrianRvoAvoidance",
            AvoidanceEnabled = UseReciprocalAvoidance,
            Use3DAvoidance = false,
            Radius = MathF.Max(.24f, _bodyHeight * .32f),
            Height = _bodyHeight,
            NeighborDistance = 2.6f,
            MaxNeighbors = 8,
            TimeHorizonAgents = 1.3f,
            MaxSpeed = 4f,
            AvoidanceLayers = 1u,
            AvoidanceMask = 1u
        };
        AddChild(_avoidance);
        _avoidance.VelocityComputed += OnAvoidanceVelocityComputed;
        // A NavigationAgent requires a valid target even when only its
        // avoidance solver is used; navigation paths remain manual.
        _avoidance.TargetPosition = _route[_routeIndex].Position;
        PlayAnimation("walk");
    }

    public override void _ExitTree()
    {
        if (_avoidance is not null && IsInstanceValid(_avoidance))
            _avoidance.VelocityComputed -= OnAvoidanceVelocityComputed;
        FinishCrossing();
    }

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

        var preferredVelocity = desired.LengthSquared() > .001f
            ? desired.Normalized() * speed : Vector3.Zero;
        // Submit every frame, including zeros: pausing pedestrians remain
        // visible to nearby RVO agents as stationary obstacles.
        if (_avoidance is not null)
        {
            // The Remote inspector can toggle the solver during an A/B test.
            if (_avoidance.AvoidanceEnabled != UseReciprocalAvoidance)
            {
                _avoidance.AvoidanceEnabled = UseReciprocalAvoidance;
                _hasAvoidanceVelocity = false;
            }
            if (UseReciprocalAvoidance) _avoidance.Velocity = preferredVelocity;
        }

        var horizontal = ResolveAvoidanceVelocity(preferredVelocity, (float)delta);
        if (horizontal.LengthSquared() > .001f)
        {
            // The Mini Characters rigs face +Z, opposite Godot's conventional -Z.
            Rotation = new Vector3(0, Mathf.Atan2(-horizontal.X, -horizontal.Z) + Mathf.Pi, 0);
        }
        var vertical = IsOnFloor() ? 0f : Velocity.Y - Gravity * (float)delta;
        Velocity = new Vector3(horizontal.X, vertical, horizontal.Z);
        MoveAndSlide();
        ReleaseCrossingAfterLeavingRoad();
    }

    public void NotifyDanger(Vector3 source, float seconds = 4.5f)
    {
        _dangerSource = source;
        _fleeRemaining = Math.Max(_fleeRemaining, seconds);
        _activityRemaining = 0;
        // Preserve the crossing reservation while fleeing on the roadway.
    }

    public void NotifyGunshot(Vector3 source)
    {
        _dangerSource = source;
        _activityRemaining = 0;
        // A startled NPC must not instantly clear an occupied crosswalk.
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
            // Crossing protection belongs to the occupied segment, not to a
            // transient animation/emotion state such as Fleeing or Startled.
            if (target.Kind == SidewalkWaypointKind.Crosswalk && _crossingController is not null)
                FinishCrossing();
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

        if (_crossingController is not null &&
            (!IsInstanceValid(_crossingController) || !_crossingController.IsInsideTree()))
            _crossingController = null;
        if (target.Kind == SidewalkWaypointKind.Crosswalk && _crossingController is null)
        {
            var controller = IntersectionTrafficController.FindNearestCrossing(GetTree(), GlobalPosition.Lerp(target.Position, .5f));
            // Missing controller means the surrounding city cell is still
            // streaming in; an unregulated crossing is not safe by default.
            if (controller is null || !controller.CanPedestrianStart(target.CrossedVehicleAxis))
            {
                State = PedestrianState.WaitingCrosswalk;
                return MoveToCrosswalkWaitingSpot(target.Position);
            }
            _crossingController = controller;
            _crossingAxis = target.CrossedVehicleAxis;
            controller.BeginPedestrianCrossing(_crossingAxis);
            State = PedestrianState.Crossing;
        }
        else State = _crossingController is null ? PedestrianState.Walking : PedestrianState.Crossing;

        PlayAnimation("walk");
        var hurry = State == PedestrianState.Crossing &&
            _crossingController?.StateForAxis(_crossingAxis) != TrafficLightState.Red ? 1.45f : 1f;
        return (toTarget.Normalized(), _walkSpeed * hurry);
    }

    private (Vector3 Direction, float Speed) MoveToCrosswalkWaitingSpot(Vector3 crossingEnd)
    {
        // Stage waiting pedestrians behind the curb, not all on one waypoint.
        // Deterministic individual offsets avoid a crowded pile of NPCs.
        var curb = _route[(_routeIndex + _route.Count - 1) % _route.Count].Position;
        var across = crossingEnd - curb;
        across.Y = 0;
        if (across.LengthSquared() < .01f) return (Vector3.Zero, 0);
        var forward = across.Normalized();
        var sideways = new Vector3(-forward.Z, 0, forward.X);
        var spot = curb - forward * (.25f + Hash01(_seed, 70) * 1.05f)
            + sideways * ((Hash01(_seed, 71) - .5f) * 1.5f);
        var approach = spot - GlobalPosition;
        approach.Y = 0;
        if (approach.LengthSquared() < .18f * .18f)
        {
            PlayAnimation("idle");
            return (Vector3.Zero, 0);
        }
        PlayAnimation("walk");
        return (approach.Normalized(), _walkSpeed * .75f);
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

    private void OnAvoidanceVelocityComputed(Vector3 safeVelocity)
    {
        safeVelocity.Y = 0f;
        _safeAvoidanceVelocity = safeVelocity;
        _hasAvoidanceVelocity = true;
    }

    private Vector3 ResolveAvoidanceVelocity(Vector3 preferred, float delta)
    {
        if (preferred.LengthSquared() < .0001f) return Vector3.Zero;
        if (!UseReciprocalAvoidance) return preferred;
        // If the navigation server has not produced a result yet, keep the
        // original deterministic sidewalk behavior (no first-frame freeze).
        if (!_hasAvoidanceVelocity) return preferred;

        var safe = _safeAvoidanceVelocity;
        safe.Y = 0f;
        // The avoidance server reports velocity, not a teleport destination.
        // Clamp to the character's existing state-dependent movement speed.
        var speed = preferred.Length();
        safe = safe.LimitLength(speed);
        // The city uses authored sidewalk segments without a baked NavMesh.
        // Some render/server configurations cannot produce avoidance velocities
        // until the navigation map has synced. Do not freeze solitary NPCs.
        if (safe.LengthSquared() < .0001f && !HasNearbyPedestrian(2.6f))
            return preferred;

        // Never walk backwards into a different sidewalk lane because of a
        // temporary congestion: waiting is safer than crossing the curb.
        if (safe.Dot(preferred) < -.01f) return Vector3.Zero;

        if (State == PedestrianState.Crossing)
            // Keep the painted crosswalk as the authoritative path. The
            // crosswalk controller, not RVO, decides vehicle right of way.
            return preferred;

        if (State == PedestrianState.Walking)
            return KeepInsideSidewalk(safe, preferred, delta);

        return safe;
    }

    private bool HasNearbyPedestrian(float radius)
    {
        var radiusSquared = radius * radius;
        foreach (var node in GetTree().GetNodesInGroup("pedestrian"))
        {
            if (node is not PedestrianAgent other || other == this ||
                !IsInstanceValid(other) || other.IsQueuedForDeletion()) continue;
            var difference = other.GlobalPosition - GlobalPosition;
            difference.Y = 0f;
            if (difference.LengthSquared() < radiusSquared) return true;
        }
        return false;
    }

    private Vector3 KeepInsideSidewalk(Vector3 safe, Vector3 preferred, float delta)
    {
        // RVO2 knows velocities, not city rules. If its proposed side-step
        // leaves the narrow sidewalk corridor, keep to the authored route.
        var from = _route[(_routeIndex + _route.Count - 1) % _route.Count].Position;
        var to = _route[_routeIndex].Position;
        var segment = to - from;
        segment.Y = 0f;
        var segmentLength = segment.LengthSquared();
        if (segmentLength < .001f) return safe;

        var next = GlobalPosition + safe * delta;
        var offset = next - from;
        offset.Y = 0f;
        var t = Mathf.Clamp(offset.Dot(segment) / segmentLength, 0f, 1f);
        var projected = from + segment * t;
        var lateral = next - projected;
        lateral.Y = 0f;
        if (lateral.LengthSquared() <= .65f * .65f) return safe;

        // Stay close to the sidewalk and let the solver try again next frame,
        // instead of taking a shortcut through buildings or the traffic lane.
        var along = segment.Normalized() * safe.Dot(segment.Normalized());
        if (along.Dot(preferred) <= .001f) return Vector3.Zero;
        return along;
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

    private void ReleaseCrossingAfterLeavingRoad()
    {
        if (_crossingController is null || State == PedestrianState.Crossing) return;
        if (!IsInstanceValid(_crossingController) || !_crossingController.IsInsideTree())
        {
            _crossingController = null;
            return;
        }

        var offset = GlobalPosition - _crossingController.GlobalPosition;
        var across = _crossingAxis == TrafficAxis.NorthSouth ? MathF.Abs(offset.X) : MathF.Abs(offset.Z);
        var along = _crossingAxis == TrafficAxis.NorthSouth ? MathF.Abs(offset.Z) : MathF.Abs(offset.X);
        // A frightened pedestrian can flee sideways, parallel to traffic.
        // Release protection once outside the junction/crosswalk area on
        // either axis, avoiding a permanently red signal.
        if (across > 5.3f || along > 7.5f) FinishCrossing();
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
