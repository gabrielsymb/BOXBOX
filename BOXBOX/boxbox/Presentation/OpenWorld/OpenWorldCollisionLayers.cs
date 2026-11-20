namespace Boxbox.Presentation.OpenWorld;

/// <summary>
/// Named 3D physics layers for the open-world presentation. Layer numbers must
/// remain synchronized with project.godot's [layer_names] section.
/// </summary>
public static class OpenWorldCollisionLayers
{
    public const uint Scenario = 1u << 0;
    public const uint TrafficVehicle = 1u << 1;
    public const uint Pedestrian = 1u << 2;
    public const uint Player = 1u << 3;
    public const uint DriveableVehicle = 1u << 4;

    public const uint AllActors = TrafficVehicle | Pedestrian | Player | DriveableVehicle;
    public const uint MovingBodyMask = Scenario | AllActors;
    public const uint TrafficSensorMask = AllActors;
}
