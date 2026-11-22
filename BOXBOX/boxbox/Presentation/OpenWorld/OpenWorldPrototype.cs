using Boxbox.Presentation.Hud;
using Godot;

namespace Boxbox.Presentation.OpenWorld;

public sealed partial class OpenWorldPrototype : Node3D
{
    private static readonly Vector3 CameraOffset = new(10.5f, 12.5f, 10.5f);

    private PlayerController? _player;
    private Camera3D? _camera;
    private Camera3D _drivingCamera = null!;
    private Camera3D _interiorCamera = null!;
    private DriveableCar _pickup = null!;
    private float _targetCameraSize = 12.5f;

    public override void _Ready()
    {
        ProcessPhysicsPriority = 100;
        GetViewport().Msaa3D = Viewport.Msaa.Msaa4X;
        ConfigureInputs();
        CreateDayNightEnvironment();
        CreateGroundCollision();

        _player = new PlayerController
        {
            Position = StartingStreet.PlayerStart
        };
        AddChild(_player);

        _pickup = new DriveableCar
        {
            Position = StartingStreet.PickupParking,
            Rotation = new Vector3(0, -Mathf.Pi / 2, 0)
        };
        AddChild(_pickup);

        var world = new OpenWorldStreamer { Player = _player };
        AddChild(world);
        var stores = StartingStreet.PlaceStores(this);

        AddChild(new TrafficSpawner { Focus = _player, World = world });
        AddChild(new PedestrianSpawner { Focus = _player });

        _camera = new Camera3D
        {
            Name = "IsometricCamera",
            Projection = Camera3D.ProjectionType.Orthogonal,
            Size = 12.5f,
            Near = 0.25f,
            Far = 70f,
            Position = _player.Position + CameraOffset
        };
        AddChild(_camera);
        _camera.LookAt(_player.FocusPosition + Vector3.Up * 0.5f);
        _camera.MakeCurrent();
        _drivingCamera = new Camera3D
        {
            Name = "PickupCamera",
            Projection = Camera3D.ProjectionType.Perspective,
            Fov = 60f,
            Near = 0.12f,
            Far = 95f
        };
        AddChild(_drivingCamera);
        _interiorCamera = new Camera3D
        {
            Name = "StoreInteriorCamera",
            Projection = Camera3D.ProjectionType.Orthogonal,
            CullMask = InteriorRenderLayer.Mask,
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Color,
                BackgroundColor = Colors.Black,
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = new Color(0.93f, 0.90f, 0.86f),
                AmbientLightEnergy = 0.53f
            }
        };
        AddChild(_interiorCamera);

        var minimap = GD.Load<PackedScene>("res://Presentation/Hud/StreetMinimap.tscn").Instantiate<StreetMinimap>();
        minimap.Player = _player;
        AddChild(minimap);
        AddChild(new StreetSupplyInteraction
        {
            Name = "SupplyRun",
            Player = _player,
            Pickup = _pickup,
            Minimap = minimap,
            Grocery = stores.Grocery,
            Supplier = stores.Supplier
        });
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_player is null || _camera is null)
        {
            return;
        }

        if (_pickup.HasDriver)
        {
            UpdateDrivingCamera((float)delta);
            return;
        }

        if (_player.Interior is { } store)
        {
            var target = store.InteriorFocus;
            _interiorCamera.Size = store.InteriorCameraSize;
            _interiorCamera.GlobalPosition = target + new Vector3(5.5f, 12f, -7f);
            _interiorCamera.LookAt(target);
            _interiorCamera.MakeCurrent();
            return;
        }

        if (!_camera.Current)
        {
            _camera.MakeCurrent();
        }
        var focus = _player.FocusPosition;
        _camera.Size = Mathf.Lerp(_camera.Size, _targetCameraSize,
            1f - Mathf.Exp(-10f * (float)delta));
        _camera.GlobalPosition = focus + CameraOffset;
        _camera.LookAt(focus + Vector3.Up * 0.78f);
    }

    private void UpdateDrivingCamera(float delta)
    {
        var forward = -_pickup.GlobalBasis.Z.Normalized();
        var focus = _pickup.GlobalPosition + Vector3.Up * 1.2f;
        var desired = focus - forward * 4.6f + Vector3.Up * 1.55f;
        var candidate = _drivingCamera.Current
            ? _drivingCamera.GlobalPosition.Lerp(desired, 1f - Mathf.Exp(-7f * delta))
            : desired;

        var query = PhysicsRayQueryParameters3D.Create(focus, candidate);
        query.Exclude = new Godot.Collections.Array<Rid> { _pickup.GetRid() };
        var obstruction = GetWorld3D().DirectSpaceState.IntersectRay(query);
        if (obstruction.TryGetValue("position", out var hit))
        {
            var offset = hit.AsVector3() - focus;
            candidate = focus + offset.Normalized() * Mathf.Max(0.12f, offset.Length() - 0.23f);
        }
        _drivingCamera.GlobalPosition = candidate;
        var aim = focus + forward * 2.6f - Vector3.Up * 0.46f;
        _drivingCamera.LookAt(aim);
        _drivingCamera.MakeCurrent();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_pickup.HasDriver || _player?.Interior is not null || @event is not InputEventMouseButton { Pressed: true } mouse) return;
        var delta = mouse.ButtonIndex switch
        {
            MouseButton.WheelUp => -1.25f,
            MouseButton.WheelDown => 1.25f,
            _ => 0f
        };
        if (delta == 0) return;
        _targetCameraSize = Mathf.Clamp(_targetCameraSize + delta, 7.5f, 18f);
        GetViewport().SetInputAsHandled();
    }

    private void CreateDayNightEnvironment()
    {
        // Construct the runtime rig from typed C# nodes. This avoids stale scene-import
        // metadata silently detaching the cycle script after hot reloads.
        var skyMaterial = new ProceduralSkyMaterial
        {
            SkyTopColor = new Color(0.36f, 0.61f, 0.82f),
            SkyHorizonColor = new Color(0.72f, 0.82f, 0.9f),
            GroundHorizonColor = new Color(0.32f, 0.38f, 0.43f),
            GroundBottomColor = new Color(0.09f, 0.12f, 0.16f)
        };
        var environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Sky,
            Sky = new Sky { SkyMaterial = skyMaterial },
            // Let the sky fill faces that do not receive direct sun. A fixed
            // blue color made the diorama look flat and disconnected from the
            // actual sky during the day/night cycle.
            AmbientLightSource = Godot.Environment.AmbientSource.Sky,
            AmbientLightSkyContribution = 0.82f,
            AmbientLightEnergy = 0.56f,
            TonemapMode = Godot.Environment.ToneMapper.Aces,
            TonemapExposure = 1.0f,
            FogEnabled = false,
            FogLightColor = new Color(0.45f, 0.54f, 0.64f),
            FogLightEnergy = 0.35f,
            FogDensity = 0.0f,
            FogSkyAffect = 0.42f
        };
        var worldEnvironment = new WorldEnvironment { Name = "WorldEnvironment", Environment = environment };
        var sun = new DirectionalLight3D { Name = "DioramaSun" };
        var cycle = new BoxboxDayNightCycle
        {
            Name = "DayNightCycle",
            WorldEnvironment = worldEnvironment,
            SunLight = sun
        };
        AddChild(cycle);
        cycle.AddChild(worldEnvironment);
        cycle.AddChild(sun);
    }

    private void CreateGroundCollision()
    {
        var ground = new StaticBody3D { Name = "InvisibleWorldGround" };
        ground.AddChild(new CollisionShape3D
        {
            Position = new Vector3(0, -0.28f, 0),
            Shape = new BoxShape3D { Size = new Vector3(2000, 0.5f, 2000) }
        });
        AddChild(ground);
    }

    private static void ConfigureInputs()
    {
        EnsureKey("move_forward", Key.W);
        EnsureKey("move_back", Key.S);
        EnsureKey("move_left", Key.A);
        EnsureKey("move_right", Key.D);
        EnsureKey("sprint", Key.Shift);
        EnsureKey("interact", Key.E);
        EnsureKey("shop_interact", Key.F);
        EnsureKey("equip_weapon", Key.Q);
        EnsureKey("fire", Key.Space);
        EnsureKey("pickup_brake", Key.Space);

        if (!InputMap.ActionGetEvents("fire").OfType<InputEventMouseButton>()
            .Any(item => item.ButtonIndex == MouseButton.Left))
        {
            InputMap.ActionAddEvent("fire", new InputEventMouseButton { ButtonIndex = MouseButton.Left });
        }
    }

    private static void EnsureKey(StringName action, Key key)
    {
        if (!InputMap.HasAction(action))
        {
            InputMap.AddAction(action);
        }

        if (InputMap.ActionGetEvents(action).OfType<InputEventKey>()
            .Any(item => item.PhysicalKeycode == key))
        {
            return;
        }

        InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
    }
}
