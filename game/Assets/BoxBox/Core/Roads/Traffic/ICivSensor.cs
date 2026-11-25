namespace BoxBox.Roads.Traffic
{
    /// <summary>What a civilian driver can see of the other vehicles (Driver scans car_data[]).</summary>
    public interface ICivSensor
    {
        /// <summary>Distance to the rear of the nearest vehicle in this car's lane ahead (MaxValue if none).</summary>
        float ObstacleAhead { get; }
        /// <summary>Nearest moving vehicle anywhere ahead (CIV_AI_THRUST_YIELD scan), MaxValue if none.</summary>
        float NearestMovingAhead { get; }
        /// <summary>No vehicle in a car-sized box one lane (3 m) to the side (CheckChangeLanes).</summary>
        bool IsSideFree(bool right);
    }

    /// <summary>Sensor with fixed readings, for tests and for an empty world.</summary>
    public sealed class FixedCivSensor : ICivSensor
    {
        public float ObstacleAhead { get; set; } = float.MaxValue;
        public float NearestMovingAhead { get; set; } = float.MaxValue;
        public bool SideFree { get; set; } = true;
        public bool IsSideFree(bool right) => SideFree;
    }
}
