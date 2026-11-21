using Godot;

namespace Boxbox.Presentation.OpenWorld;

public sealed partial class DriveableCar : CharacterBody3D
{
    private const float MaximumForwardSpeed = 12f;
    private const float MaximumReverseSpeed = 4f;
    private const float Acceleration = 8f;
    private const float Braking = 12f;
    private const float RollingResistance = 4f;
    private const float Wheelbase = 2.12f;
    private const float HandbrakeDeceleration = 21f;
    private const float DriveTorqueAgainstBrake = 3.5f;
    private const float SteeringResponse = 3.2f;
    private const float Gravity = 18f;
    private const float TireRadius = 0.315f;
    private const float LateralGrip = 16f;
    private const float HandbrakeGrip = 2.4f;

    private PlayerController? _driver;
    private float _steeringAngle;
    private Node3D? _visual;
    private readonly List<(Node3D Steering, Node3D Wheel, Basis InitialBasis)> _wheels = [];
    private float _wheelSpin;
    private float _rearWheelSpin;
    private SpotLight3D? _leftHeadlight;
    private SpotLight3D? _rightHeadlight;
    private VehicleLightMaterialController? _lightMaterials;
    private BoxboxDayNightCycle? _cycle;

    private PickupCargo _cargo = null!;
    private PickupSkidMarks _skidMarks = null!;
    private Vector3 _previousActualVelocity;

    public bool HasDriver => _driver is not null;
    public bool IsParked => !HasDriver && new Vector2(Velocity.X, Velocity.Z).LengthSquared() < 0.1f;
    public void ShowCargo(int units) => _cargo.ShowUnits(units);

    public override void _Ready()
    {
        Name = "ProtagonistPickup";
        AddToGroup("driveable_car");
        _cargo = new PickupCargo { Name = "CargoBed" };
        AddChild(_cargo);
        _skidMarks = new PickupSkidMarks { Name = "PickupSkidMarks", TopLevel = true };
        AddChild(_skidMarks);
        _skidMarks.GlobalTransform = Transform3D.Identity;
        CollisionMask |= 1u << 1;

        var collision = new CollisionShape3D
        {
            Name = "CarCollision",
            // The bottom coincides with the wheel contact plane of the visual.
            Position = new Vector3(0, 0.6f, 0),
            Shape = new BoxShape3D { Size = new Vector3(1.62f, 1.2f, 3.52f) }
        };
        AddChild(collision);

        var scene = GD.Load<PackedScene>("res://Assets/World/PlayerVehicles/player-pickup-textured.glb");
        _visual = scene.Instantiate<Node3D>();
        _visual.Name = "PickupVisual";
        _visual.Scale = Vector3.One * 0.72f;
        _visual.RotateY(Mathf.Pi);
        VisualShadowSupport.AlignLowestPointToGround(_visual, 0.004f);
        AddChild(_visual);
        VisualShadowSupport.SetCastShadows(_visual);
        foreach (var mesh in _visual.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
        {
            for (var surface = 0; surface < mesh.GetSurfaceOverrideMaterialCount(); surface++)
            {
                if (mesh.GetActiveMaterial(surface) is StandardMaterial3D source && source.ResourceName == "Material_0")
                {
                    var material = (StandardMaterial3D)source.Duplicate();
                    material.VertexColorUseAsAlbedo = true;
                    mesh.SetSurfaceOverrideMaterial(surface, material);
                }
            }
        }
        foreach (var pivot in _visual.FindChildren("wheel-*", "Node3D", true, false).OfType<Node3D>())
        {
            if (pivot.GetChildCount() == 1 && pivot.GetChild(0) is Node3D wheel)
            {
                _wheels.Add((pivot, wheel, wheel.Basis));
            }
        }
        _lightMaterials = new VehicleLightMaterialController { Name = "VehicleLightMaterials" };
        AddChild(_lightMaterials);
        _lightMaterials.Bind(_visual);
        CreateHeadlights();
        ConnectDayNightCycle();
        AddChild(VisualShadowSupport.CreateContactShadow("VehicleContactShadow", new Vector2(1.62f, 3.24f)));
    }

    private void CreateHeadlights()
    {
        _leftHeadlight = CreateHeadlight("PlayerLeftHeadlight", -0.60f);
        _rightHeadlight = CreateHeadlight("PlayerRightHeadlight", 0.60f);
        AddChild(_leftHeadlight);
        AddChild(_rightHeadlight);
    }

    private static SpotLight3D CreateHeadlight(string name, float x) => new()
    {
        Name = name,
        Position = new Vector3(x, 0.70f, -1.72f),
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
        if (GetTree().GetFirstNodeInGroup("day_night_cycle") is not BoxboxDayNightCycle cycle)
        {
            SetHeadlights(false);
            return;
        }
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

    private void SetHeadlights(bool night)
    {
        var beamEnergy = night ? 3f : 0.42f;
        if (_leftHeadlight is not null)
        {
            _leftHeadlight.Visible = true;
            _leftHeadlight.LightEnergy = beamEnergy;
        }
        if (_rightHeadlight is not null)
        {
            _rightHeadlight.Visible = true;
            _rightHeadlight.LightEnergy = beamEnergy;
        }
        _lightMaterials?.SetHeadlights(true, night ? 2.5f : 0.55f);
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
        var controlled = _driver is not null;
        var throttle = controlled ? Input.GetAxis("move_back", "move_forward") : 0f;
        var steering = controlled ? Input.GetAxis("move_right", "move_left") : 0f;
        var handbrake = controlled && Input.IsActionPressed("pickup_brake");
        if (handbrake)
        {
            // Held throttle loads the brake, but cannot propel the pickup through it.
            var brakeForce = HandbrakeDeceleration - DriveTorqueAgainstBrake * Mathf.Abs(throttle);
            forwardSpeed = Mathf.MoveToward(forwardSpeed, 0, brakeForce * step);
        }
        else if (Mathf.IsZeroApprox(throttle))
        {
            forwardSpeed = Mathf.MoveToward(forwardSpeed, 0, RollingResistance * step);
        }
        else if (Mathf.Abs(forwardSpeed) > 0.15f && Mathf.Sign(forwardSpeed) != Mathf.Sign(throttle))
        {
            forwardSpeed = Mathf.MoveToward(forwardSpeed, 0, Braking * step);
        }
        else
        {
            var targetSpeed = throttle * (throttle > 0 ? MaximumForwardSpeed : MaximumReverseSpeed);
            forwardSpeed = Mathf.MoveToward(forwardSpeed, targetSpeed, Acceleration * step);
        }

        var speedRatio = Mathf.Clamp(Mathf.Abs(forwardSpeed) / MaximumForwardSpeed, 0, 1);
        var maxSteer = Mathf.Lerp(Mathf.DegToRad(35), Mathf.DegToRad(13), speedRatio);
        _steeringAngle = Mathf.MoveToward(_steeringAngle, steering * maxSteer, SteeringResponse * step);
        var momentum = forward * forwardSpeed + right * lateralSpeed;
        if (IsOnFloor())
        {
            RotateY(forwardSpeed * Mathf.Tan(_steeringAngle) / Wheelbase * step);
        }

        forward = -GlobalTransform.Basis.Z.Normalized();
        right = GlobalTransform.Basis.X.Normalized();
        forwardSpeed = momentum.Dot(forward);
        lateralSpeed = Mathf.MoveToward(momentum.Dot(right), 0,
            (handbrake ? HandbrakeGrip : LateralGrip) * step);
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
        var actualVelocity = GetRealVelocity();
        var localAcceleration = GlobalBasis.Inverse() * ((actualVelocity - _previousActualVelocity) / step);
        _previousActualVelocity = actualVelocity;
        _cargo.Step(localAcceleration, step);
        var skidding = handbrake && IsOnFloor() && new Vector2(actualVelocity.X, actualVelocity.Z).Length() > 2f;
        _skidMarks.UpdateTracks(this, skidding);
        AnimateWheels(forwardSpeed, step, handbrake);
        AnimateBodyRoll(steering, forwardSpeed, step);
    }

    private void AnimateWheels(float forwardSpeed, float delta, bool handbrake)
    {
        _wheelSpin = Mathf.Wrap(_wheelSpin + forwardSpeed * delta / TireRadius, 0, Mathf.Tau);
        if (!handbrake)
        {
            _rearWheelSpin = Mathf.Wrap(_rearWheelSpin + forwardSpeed * delta / TireRadius, 0, Mathf.Tau);
        }

        foreach (var (pivot, wheel, rest) in _wheels)
        {
            var front = pivot.Name.ToString().StartsWith("wheel-f", StringComparison.Ordinal);
            if (front)
            {
                pivot.Rotation = new Vector3(0, _steeringAngle, 0);
            }
            wheel.Basis = rest * new Basis(Vector3.Right, front ? -_wheelSpin : -_rearWheelSpin);
        }
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
        if (_driver is not null || player.Interior is not null)
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
