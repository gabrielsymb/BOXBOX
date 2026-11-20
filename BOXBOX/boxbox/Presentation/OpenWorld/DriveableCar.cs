using Godot;

namespace Boxbox.Presentation.OpenWorld;

public sealed partial class DriveableCar : CharacterBody3D
{
    private const float MaximumForwardSpeed = 12f;
    private const float MaximumReverseSpeed = 4f;
    private const float Acceleration = 8f;
    private const float Braking = 12f;
    private const float RollingResistance = 4f;
    private const float SteeringRate = 1.8f;
    private const float Gravity = 18f;
    private const float TireRadius = 0.3f;
    private const float LateralGrip = 16f;
    private const float HandbrakeGrip = 2.4f;

    private PlayerController? _driver;
    private float _speed;
    private Node3D? _visual;
    private readonly List<Node3D> _wheels = [];
    private SpotLight3D? _leftHeadlight;
    private SpotLight3D? _rightHeadlight;
    private VehicleLightMaterialController? _lightMaterials;
    private BoxboxDayNightCycle? _cycle;

    public bool HasDriver => _driver is not null;

    public override void _Ready()
    {
        Name = "DriveableSedan";
        AddToGroup("driveable_car");
        CollisionMask |= 1u << 1;

        var collision = new CollisionShape3D
        {
            Name = "CarCollision",
            // A little clearance under the chassis keeps the box from catching the
            // seam where a ramp meets a raised lot.
            Position = new Vector3(0, 0.68f, 0),
            Shape = new BoxShape3D { Size = new Vector3(1.5f, 1.1f, 3.25f) }
        };
        AddChild(collision);

        var scene = GD.Load<PackedScene>("res://Assets/World/Vehicles/sedan.glb");
        _visual = scene.Instantiate<Node3D>();
        _visual.Name = "ApprovedSedanVisual";
        _visual.RotateY(Mathf.Pi);
        VisualShadowSupport.AlignLowestPointToGround(_visual);
        AddChild(_visual);
        VisualShadowSupport.SetCastShadows(_visual);
        CollectWheels(_visual);
        _lightMaterials = new VehicleLightMaterialController { Name = "VehicleLightMaterials" };
        AddChild(_lightMaterials);
        _lightMaterials.Bind(_visual);
        CreateHeadlights();
        ConnectDayNightCycle();
        AddChild(VisualShadowSupport.CreateContactShadow("VehicleContactShadow", new Vector2(1.45f, 2.7f)));
    }

    private void CreateHeadlights()
    {
        _leftHeadlight = CreateHeadlight("PlayerLeftHeadlight", -0.46f);
        _rightHeadlight = CreateHeadlight("PlayerRightHeadlight", 0.46f);
        AddChild(_leftHeadlight);
        AddChild(_rightHeadlight);
    }

    private static SpotLight3D CreateHeadlight(string name, float x) => new()
    {
        Name = name,
        Position = new Vector3(x, 0.58f, -1.48f),
        RotationDegrees = new Vector3(-12f, 0, 0),
        LightColor = Color.FromHtml("fff4cc"),
        LightEnergy = 3f,
        SpotRange = 10f,
        SpotAngle = 29f,
        // Only the single player vehicle pays for headlight shadows. NPC
        // headlights remain shadowless to keep traffic scalable.
        ShadowEnabled = true,
        Visible = false
    };

    private void ConnectDayNightCycle()
    {
        if (GetTree().GetFirstNodeInGroup("day_night_cycle") is not BoxboxDayNightCycle cycle) return;
        _cycle = cycle;
        cycle.DayStarted += OnDayStarted;
        cycle.NightStarted += OnNightStarted;
        SetHeadlights(cycle.IsNight());
    }

    public override void _ExitTree()
    {
        if (_cycle is null) return;
        _cycle.DayStarted -= OnDayStarted;
        _cycle.NightStarted -= OnNightStarted;
        _cycle = null;
    }

    private void OnDayStarted() => SetHeadlights(false);
    private void OnNightStarted() => SetHeadlights(true);

    private void SetHeadlights(bool enabled)
    {
        if (_leftHeadlight is not null) _leftHeadlight.Visible = enabled;
        if (_rightHeadlight is not null) _rightHeadlight.Visible = enabled;
        _lightMaterials?.SetHeadlights(enabled);
    }

    public override void _PhysicsProcess(double delta)
    {
        var step = (float)delta;
        var forward = -GlobalTransform.Basis.Z.Normalized();
        var right = GlobalTransform.Basis.X.Normalized();
        var horizontal = new Vector3(Velocity.X, 0, Velocity.Z);
        var forwardSpeed = horizontal.Dot(forward);
        var previousForwardSpeed = forwardSpeed;
        var lateralSpeed = horizontal.Dot(right);
        var steering = 0f;
        var throttle = 0f;
        var handbrake = _driver is not null && Input.IsActionPressed("fire");

        if (_driver is not null)
        {
            throttle = Input.GetAxis("move_back", "move_forward");
            if (throttle > 0)
            {
                forwardSpeed = Mathf.MoveToward(forwardSpeed, MaximumForwardSpeed, Acceleration * step);
            }
            else if (throttle < 0)
            {
                forwardSpeed = Mathf.MoveToward(forwardSpeed, -MaximumReverseSpeed, Braking * step);
            }
            else
            {
                forwardSpeed = Mathf.MoveToward(forwardSpeed, 0, RollingResistance * step);
            }

            steering = Input.GetAxis("move_right", "move_left");
            if (MathF.Abs(forwardSpeed) > 0.25f)
            {
                var steeringStrength = Mathf.Clamp(Mathf.Abs(forwardSpeed) / MaximumForwardSpeed, 0f, 1f);
                RotateY(steering * SteeringRate * steeringStrength * Mathf.Sign(forwardSpeed) * step);
            }
        }
        else
        {
            forwardSpeed = Mathf.MoveToward(forwardSpeed, 0, RollingResistance * step);
        }

        lateralSpeed = Mathf.MoveToward(lateralSpeed, 0,
            (handbrake ? HandbrakeGrip : LateralGrip) * step);
        forward = -GlobalTransform.Basis.Z.Normalized();
        right = GlobalTransform.Basis.X.Normalized();
        _speed = forwardSpeed;
        var slowingDown = MathF.Abs(forwardSpeed) < MathF.Abs(previousForwardSpeed) - 0.01f;
        var reversing = forwardSpeed < -0.1f || throttle < -0.05f;
        _lightMaterials?.SetTaillights(handbrake || slowingDown || reversing);
        var planar = forward * forwardSpeed + right * lateralSpeed;
        Velocity = new Vector3(planar.X, Velocity.Y, planar.Z);
        if (!IsOnFloor())
        {
            Velocity = new Vector3(Velocity.X, Velocity.Y - Gravity * (float)delta, Velocity.Z);
        }

        MoveAndSlide();
        AnimateWheels(forwardSpeed, step);
        AnimateBodyRoll(steering, forwardSpeed, step);
    }

    private void CollectWheels(Node node)
    {
        if (node is Node3D node3D && node.Name.ToString().Contains("wheel", StringComparison.OrdinalIgnoreCase))
            _wheels.Add(node3D);
        foreach (var child in node.GetChildren()) CollectWheels(child);
    }

    private void AnimateWheels(float forwardSpeed, float delta)
    {
        var spin = forwardSpeed * delta / TireRadius;
        foreach (var wheel in _wheels) wheel.RotateX(spin);
    }

    private void AnimateBodyRoll(float steering, float forwardSpeed, float delta)
    {
        if (_visual is null) return;
        var speedFactor = Mathf.Clamp(MathF.Abs(forwardSpeed) / MaximumForwardSpeed, 0, 1);
        var target = -steering * speedFactor * 0.1f;
        var rotation = _visual.Rotation;
        rotation.Z = Mathf.Lerp(rotation.Z, target, 7f * delta);
        _visual.Rotation = rotation;
    }

    public void Enter(PlayerController player)
    {
        if (_driver is not null)
        {
            return;
        }

        _driver = player;
        player.BeginDriving(this);
    }

    public void Exit(PlayerController player)
    {
        if (_driver == player)
        {
            _driver = null;
        }
    }
}
