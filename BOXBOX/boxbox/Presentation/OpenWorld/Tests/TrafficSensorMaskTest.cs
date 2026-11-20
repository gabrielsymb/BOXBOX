using Godot;

namespace Boxbox.Presentation.OpenWorld.Tests;

/// <summary>
/// Verifies that the traffic ray ignores a nearer scenario collider and still
/// detects a dynamic actor behind it.
/// </summary>
public sealed partial class TrafficSensorMaskTest : Node3D
{
    private TrafficVehicle? _vehicle;
    private int _physicsFrames;

    public override void _Ready()
    {
        AddProbe("ScenarioPoleProbe", new Vector3(0f, 0.62f, 2.1f),
            OpenWorldCollisionLayers.Scenario);
        AddProbe("DynamicVehicleProbe", new Vector3(0f, 0.62f, 4.1f),
            OpenWorldCollisionLayers.TrafficVehicle);

        _vehicle = new TrafficVehicle { Position = new Vector3(0f, 0.35f, 0f) };
        var visualPath = WorldAssetCatalog.Current.RequireByStem(WorldAssetCategory.Vehicles, "sedan");
        _vehicle.Initialize(Vector3.Back, visualPath, 500u);
        AddChild(_vehicle);
    }

    public override void _PhysicsProcess(double delta)
    {
        _physicsFrames++;
        if (_physicsFrames < 3 || _vehicle is null) return;

        var detected = _vehicle.CenterSensorColliderName;
        var passed = detected == "DynamicVehicleProbe";
        GD.Print($"TRAFFIC_SENSOR_MASK_TEST resultado={(passed ? "PASS" : "FAIL")} " +
                 $"detectado={detected} cenario_ignorado={passed.ToString().ToLowerInvariant()}");
        GetTree().Quit(passed ? 0 : 1);
    }

    private void AddProbe(string name, Vector3 position, uint layer)
    {
        var body = new StaticBody3D
        {
            Name = name,
            Position = position,
            CollisionLayer = layer,
            CollisionMask = 0
        };
        body.AddChild(new CollisionShape3D
        {
            Shape = new BoxShape3D { Size = new Vector3(0.8f, 1f, 0.8f) }
        });
        AddChild(body);
    }
}
