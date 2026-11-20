using Godot;

namespace Boxbox.Presentation.OpenWorld;

public enum TrafficTaskState { Cruising, Braking, Avoiding, WaitingForJunction, InsideJunction }

public sealed partial class TrafficVehicle : CharacterBody3D
{
    private const float CruiseSpeed = 6.2f;
    private const float CornerSpeed = 3.7f;
    private const float Acceleration = 4.5f;
    private const float Braking = 11f;
    private const float SteeringRate = 2.15f;
    private const float Gravity = 18f;
    private const float SensorLength = 5.8f;
    private readonly List<Vector3> _waypoints = [];
    private readonly Queue<RoadManeuver> _maneuvers = [];
    private Vector3 _routeDirection = Vector3.Forward;
    private float _speed;
    private float _avoidOffset;
    private Vector3 _avoidTarget;
    private double _blockedSeconds;
    private double _deadlockSeconds;
    private bool _deadlockReported;
    private string _centerColliderName = "none";
    private string _nearestIntersectionStatus = "none";
    private uint _routeSeed;
    private uint _maneuverIndex;
    private SpotLight3D? _leftHeadlight;
    private SpotLight3D? _rightHeadlight;
    private double _headlightDelay;
    private bool _headlightsRequested;
    private VehicleLightMaterialController? _lightMaterials;
    private BoxboxDayNightCycle? _cycle;
    private Label3D? _debugLabel;
    private IntersectionTrafficController? _junctionController;

    public TrafficTaskState TaskState { get; private set; } = TrafficTaskState.Cruising;
    public uint DiagnosticId => _routeSeed;
    public string CenterSensorColliderName => _centerColliderName;
    public float CurrentSpeed => _speed;
    public Vector3 ActiveIncomingDirection => _maneuvers.Count > 0
        ? _maneuvers.Peek().IncomingDirection
        : _routeDirection;
    public event Action<TrafficVehicle>? DeadlockDetected;

    public void SetDebugOverlayVisible(bool visible)
    {
        if (_debugLabel is not null) _debugLabel.Visible = visible;
    }

    public void Initialize(Vector3 direction, string visualPath, uint routeSeed)
    {
        _routeDirection = direction.Normalized();
        _routeSeed = routeSeed;
        Rotation = new Vector3(0, Mathf.Atan2(-_routeDirection.X, -_routeDirection.Z), 0);
        var visualScene = GD.Load<PackedScene>(visualPath);
        var visual = visualScene.Instantiate<Node3D>();
        visual.Name = "TrafficVehicleVisual";
        visual.RotateY(Mathf.Pi);
        VisualShadowSupport.AlignLowestPointToGround(visual);
        AddChild(visual);
        VisualShadowSupport.SetCastShadows(visual);
        _lightMaterials = new VehicleLightMaterialController { Name = "VehicleLightMaterials" };
        AddChild(_lightMaterials);
        _lightMaterials.Bind(visual);
    }

    public override void _Ready()
    {
        Name = "TrafficVehicle";
        AddToGroup("traffic_vehicle");
        CollisionLayer = OpenWorldCollisionLayers.TrafficVehicle;
        CollisionMask = OpenWorldCollisionLayers.MovingBodyMask;
        AddChild(new CollisionShape3D
        {
            Name = "TrafficCollision",
            Position = new Vector3(0, 0.62f, 0),
            Shape = new BoxShape3D { Size = new Vector3(1.45f, 1.18f, 3.1f) }
        });
        CreateHeadlights();
        ConnectDayNightCycle();
        EnsureRoute();
        CreateDebugLabel();
        AddChild(VisualShadowSupport.CreateContactShadow("VehicleContactShadow", new Vector2(1.4f, 2.55f)));
    }

    public override void _PhysicsProcess(double delta)
    {
        UpdateHeadlights(delta);
        AdvanceReachedWaypoints();
        AdvanceCompletedManeuvers();
        EnsureRoute();

        var insideJunction = _junctionController?.IsInside(this) == true;

        var forward = -GlobalTransform.Basis.Z.Normalized();
        SenseObstacles(forward, out var leftDistance, out var centerDistance, out var rightDistance);
        UpdateAvoidance(delta, leftDistance, centerDistance, rightDistance);

        var target = _waypoints[0];
        var desired = target - GlobalPosition;
        desired.Y = 0;
        var right = forward.Cross(Vector3.Up).Normalized();
        // Avoidance is a point ahead in the neighbouring part of the lane. The vehicle
        // must steer its body towards it; its velocity is never translated sideways.
        if (MathF.Abs(_avoidOffset) > 0.01f)
        {
            _avoidTarget = GlobalPosition + forward * 5.5f + right * _avoidOffset;
            desired = _avoidTarget - GlobalPosition;
        }
        if (desired.LengthSquared() > 0.001f) desired = desired.Normalized();

        var signedAngle = forward.SignedAngleTo(desired, Vector3.Up);
        var speedSteering = Mathf.Clamp(_speed / 2.2f, 0.18f, 1f);
        RotateY(Mathf.Clamp(signedAngle, -SteeringRate * speedSteering * (float)delta,
            SteeringRate * speedSteering * (float)delta));
        forward = -GlobalTransform.Basis.Z.Normalized();

        var cornering = MathF.Abs(signedAngle) > 0.22f;
        var obstacleFactor = Mathf.Clamp((centerDistance - 1.15f) / (SensorLength - 1.15f), 0, 1);
        var targetSpeed = (cornering ? CornerSpeed : CruiseSpeed) * obstacleFactor;
        if (centerDistance < 1.3f) targetSpeed = 0;
        var axis = IntersectionTrafficController.AxisFor(forward);
        var intersection = IntersectionTrafficController.FindApproaching(GetTree(), GlobalPosition, forward,
            out var stopDistance);
        var stoppedBySignal = intersection?.ShouldVehicleStop(axis, stopDistance) == true;
        var waitingForAdmission = false;
        if (intersection is not null && !stoppedBySignal && stopDistance <= 2.5f)
        {
            var maneuver = FindManeuver(intersection.GlobalPosition);
            if (maneuver is not null && intersection.TryEnter(this, axis, maneuver.Value.ExitDirection))
                _junctionController = intersection;
            else
                waitingForAdmission = true;
        }

        if (stoppedBySignal || waitingForAdmission)
        {
            var stopFactor = Mathf.Clamp((stopDistance - .45f) / 4.5f, 0f, 1f);
            targetSpeed = MathF.Min(targetSpeed, CruiseSpeed * stopFactor);
            if (stopDistance < .7f) targetSpeed = 0;
        }
        var yieldingInside = insideJunction && _junctionController?.MayProceedInside(this) == false;
        if (yieldingInside) targetSpeed = 0;

        if (insideJunction) TaskState = TrafficTaskState.InsideJunction;
        else if (waitingForAdmission) TaskState = TrafficTaskState.WaitingForJunction;

        var previousSpeed = _speed;
        _speed = Mathf.MoveToward(_speed, targetSpeed,
            (targetSpeed < _speed ? Braking : Acceleration) * (float)delta);
        _lightMaterials?.SetTaillights(_speed < previousSpeed - 0.01f);

        UpdateIntersectionStatus(axis);
        UpdateDeadlockObservation(delta, stoppedBySignal || waitingForAdmission || yieldingInside);
        UpdateDebugLabel();

        Velocity = new Vector3(
            forward.X * _speed,
            IsOnFloor() ? 0 : Velocity.Y - Gravity * (float)delta,
            forward.Z * _speed);
        MoveAndSlide();
    }

    public void OnEnteredIntersection(IntersectionTrafficController controller)
    {
        _junctionController = controller;
    }

    public void OnExitedIntersection(IntersectionTrafficController controller)
    {
        if (_junctionController == controller) _junctionController = null;
    }

    public void OnAdmissionExpired(IntersectionTrafficController controller)
    {
        if (_junctionController == controller && !controller.IsInside(this)) _junctionController = null;
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.F1 }) return;
        if (_debugLabel is not null) SetDebugOverlayVisible(!_debugLabel.Visible);
    }

    private void CreateDebugLabel()
    {
        _debugLabel = new Label3D
        {
            Name = "TrafficDebugLabel",
            Position = new Vector3(0, 2.05f, 0),
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            FixedSize = true,
            FontSize = 28,
            OutlineSize = 8,
            PixelSize = 0.0025f,
            NoDepthTest = true,
            Visible = false
        };
        AddChild(_debugLabel);
    }

    private void UpdateIntersectionStatus(TrafficAxis axis)
    {
        var nearest = IntersectionTrafficController.FindNearestCrossing(GetTree(), GlobalPosition, 24f);
        _nearestIntersectionStatus = nearest is null ? "none" : $"{axis}/{nearest.Phase}";
    }

    private void UpdateDeadlockObservation(double delta, bool stoppedBySignal)
    {
        var unexpectedlyStopped = _speed < 0.2f && !stoppedBySignal;
        if (!unexpectedlyStopped)
        {
            _deadlockSeconds = 0;
            _deadlockReported = false;
            return;
        }

        _deadlockSeconds += delta;
        if (_deadlockSeconds <= 5.0 || _deadlockReported) return;

        _deadlockReported = true;
        var position = GlobalPosition;
        GD.Print($"DEADLOCK id={DiagnosticId} pos={position.X:F2},{position.Z:F2} " +
                 $"bloqueado_por={_centerColliderName} fase={_nearestIntersectionStatus} " +
                 $"tempo={_deadlockSeconds:F1}s");
        DeadlockDetected?.Invoke(this);
    }

    private void UpdateDebugLabel()
    {
        if (_debugLabel is null || !_debugLabel.Visible) return;
        _debugLabel.Text = $"id={DiagnosticId} state={TaskState}\n" +
                           $"speed={_speed:F2} blocked={_blockedSeconds:F1}s\n" +
                           $"hit={_centerColliderName}\nintersection={_nearestIntersectionStatus}";
    }

    private void CreateHeadlights()
    {
        _leftHeadlight = CreateHeadlight("LeftHeadlight", -0.46f);
        _rightHeadlight = CreateHeadlight("RightHeadlight", 0.46f);
        AddChild(_leftHeadlight);
        AddChild(_rightHeadlight);
    }

    private static SpotLight3D CreateHeadlight(string name, float x) => new()
    {
        Name = name,
        Position = new Vector3(x, 0.58f, -1.48f),
        RotationDegrees = new Vector3(-12f, 0, 0),
        LightColor = Color.FromHtml("fff4cc"),
        LightEnergy = 2.6f,
        SpotRange = 9.5f,
        SpotAngle = 29f,
        ShadowEnabled = false,
        Visible = false
    };

    private void ConnectDayNightCycle()
    {
        if (GetTree().GetFirstNodeInGroup("day_night_cycle") is not BoxboxDayNightCycle cycle) return;
        _cycle = cycle;
        cycle.DayStarted += OnDayStarted;
        cycle.NightStarted += OnNightStarted;
        ScheduleHeadlights(cycle.IsNight());
    }

    public override void _ExitTree()
    {
        if (_cycle is null) return;
        _cycle.DayStarted -= OnDayStarted;
        _cycle.NightStarted -= OnNightStarted;
        _cycle = null;
    }

    private void OnDayStarted() => ScheduleHeadlights(false);
    private void OnNightStarted() => ScheduleHeadlights(true);

    private void ScheduleHeadlights(bool enabled)
    {
        _headlightsRequested = enabled;
        // Stable per vehicle: avoids a synchronized light spike without consuming
        // the economic simulation's random source.
        _headlightDelay = 0.1 + (_routeSeed % 1401) / 1000.0;
    }

    private void UpdateHeadlights(double delta)
    {
        if (_headlightDelay <= 0) return;
        _headlightDelay -= delta;
        if (_headlightDelay > 0) return;
        if (_leftHeadlight is not null) _leftHeadlight.Visible = _headlightsRequested;
        if (_rightHeadlight is not null) _rightHeadlight.Visible = _headlightsRequested;
        _lightMaterials?.SetHeadlights(_headlightsRequested);
    }

    private void AdvanceReachedWaypoints()
    {
        while (_waypoints.Count > 0 && GlobalPosition.DistanceTo(_waypoints[0]) < 0.75f)
            _waypoints.RemoveAt(0);
    }

    private void AdvanceCompletedManeuvers()
    {
        while (_maneuvers.Count > 0 && _maneuvers.Peek().HasExited(GlobalPosition))
            _maneuvers.Dequeue();
    }

    private void EnsureRoute()
    {
        if (_waypoints.Count > 2) return;
        var from = _waypoints.Count > 0 ? _waypoints[^1] : GlobalPosition;
        var maneuver = RoadNodeGraph.AppendNextManeuver(
            _waypoints, from, ref _routeDirection, _routeSeed + _maneuverIndex * 7919);
        _maneuvers.Enqueue(maneuver);
        _maneuverIndex++;
    }

    private RoadManeuver? FindManeuver(Vector3 intersectionCenter)
    {
        foreach (var maneuver in _maneuvers)
        {
            var offset = maneuver.IntersectionCenter - intersectionCenter;
            offset.Y = 0;
            if (offset.LengthSquared() < 1f) return maneuver;
        }
        return null;
    }

    private void UpdateAvoidance(double delta, float left, float center, float right)
    {
        if (center < 2.8f && _speed < 1.2f) _blockedSeconds += delta;
        else _blockedSeconds = Math.Max(0, _blockedSeconds - delta * 2);

        if (_blockedSeconds > 2.2 && MathF.Max(left, right) > center + 1.6f)
        {
            TaskState = TrafficTaskState.Avoiding;
            var desiredOffset = left > right ? -0.72f : 0.72f;
            _avoidOffset = Mathf.MoveToward(_avoidOffset, desiredOffset, 1.2f * (float)delta);
        }
        else
        {
            TaskState = center < SensorLength ? TrafficTaskState.Braking : TrafficTaskState.Cruising;
            _avoidOffset = Mathf.MoveToward(_avoidOffset, 0, 0.8f * (float)delta);
        }
    }

    private void SenseObstacles(Vector3 forward, out float left, out float center, out float right)
    {
        center = CastSensor(forward, out _centerColliderName);
        left = CastSensor(new Basis(Vector3.Up, Mathf.DegToRad(24)) * forward, out _);
        right = CastSensor(new Basis(Vector3.Up, Mathf.DegToRad(-24)) * forward, out _);
    }

    private float CastSensor(Vector3 direction, out string colliderName)
    {
        var origin = GlobalPosition + Vector3.Up * 0.62f + direction * 0.65f;
        var query = PhysicsRayQueryParameters3D.Create(
            origin, origin + direction * SensorLength, OpenWorldCollisionLayers.TrafficSensorMask);
        var exclusions = new Godot.Collections.Array<Rid> { GetRid() };
        _junctionController?.AppendInsideVehicleExclusions(exclusions, this);
        query.Exclude = exclusions;
        var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
        if (hit.Count == 0)
        {
            colliderName = "none";
            return SensorLength;
        }

        colliderName = hit["collider"].AsGodotObject() is Node collider
            ? collider.Name.ToString()
            : "unknown";
        return origin.DistanceTo(hit["position"].AsVector3());
    }
}
