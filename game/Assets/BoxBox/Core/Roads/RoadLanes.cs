namespace BoxBox.Roads
{
    /// <summary>
    /// Lane layout of one road piece. Ported from REDRIVER2 (MIT, SoapyMan) src_rebuild/Game/C/dr2roads.h:
    /// NumLanes (lane count per half + speed limit id + parking bits), LaneDirs and AILanes.
    /// Lanes are numbered from the right-hand edge of the piece's heading (lane 0) to the left edge.
    /// </summary>
    public readonly struct RoadLanes
    {
        /// <summary>512 Driver units per lane × 0.006 m/U (scale from the vehicle port).</summary>
        public const float LaneWidthM = 512f * 0.006f;

        /// <summary>civ_ai.c speedLimits {56, 97, 138} U/step converted to km/h.</summary>
        private static readonly float[] SpeedLimitsKmh = { 36f, 63f, 89f };

        /// <summary>Half the lane count (Driver's ROAD_LANES_COUNT, 4 bits).</summary>
        public int HalfCount { get; }
        /// <summary>Bit i = direction of lanes 2i and 2i+1 (1 = against the piece heading).</summary>
        public int LaneDirs { get; }
        /// <summary>Bit i = lanes 2i and 2i+1 can be driven by civilian AI.</summary>
        public int AiLanes { get; }
        public int SpeedLimitId { get; }
        public bool LeftmostParking { get; }
        public bool RightmostParking { get; }

        public RoadLanes(int halfCount, int laneDirs, int aiLanes, int speedLimitId,
            bool leftmostParking = false, bool rightmostParking = false)
        {
            HalfCount = halfCount < 1 ? 1 : halfCount > 15 ? 15 : halfCount;
            LaneDirs = laneDirs & 0xFF;
            AiLanes = aiLanes & 0xFF;
            SpeedLimitId = speedLimitId < 0 ? 0 : speedLimitId > 3 ? 3 : speedLimitId;
            LeftmostParking = leftmostParking;
            RightmostParking = rightmostParking;
        }

        /// <summary>Two-way street, one lane each way (Driver encodes it as LaneDirs 0xFF, 1 lane).</summary>
        public static RoadLanes TwoWay(int speedLimitId = 0) => new RoadLanes(1, 0xFF, 0xFF, speedLimitId);

        /// <summary>Two-way avenue, two lanes each way: lanes 0-1 with the heading, 2-3 against it.</summary>
        public static RoadLanes Avenue(int speedLimitId = 1) => new RoadLanes(2, 0b10, 0xFF, speedLimitId);

        /// <summary>ROAD_WIDTH_IN_LANES.</summary>
        public int Count => HalfCount * 2;
        public float WidthM => Count * LaneWidthM;
        public float SpeedLimitKmh => SpeedLimitsKmh[SpeedLimitId > 2 ? 2 : SpeedLimitId];

        /// <summary>ROAD_IS_AI_LANE.</summary>
        public bool IsAiLane(int lane) => ((AiLanes >> (lane / 2)) & 1) != 0;

        /// <summary>ROAD_LANE_DIR: true when the lane runs against the piece heading.</summary>
        public bool IsReversed(int lane)
        {
            if (LaneDirs == 0xFF && HalfCount == 1) return (lane & 1) != 0;
            return ((LaneDirs >> (lane / 2)) & 1) != 0;
        }

        /// <summary>ROAD_IS_PARKING_ALLOWED_AT.</summary>
        public bool IsParkingAllowedAt(int lane) =>
            (LeftmostParking && lane == 0) || (RightmostParking && lane == Count - 1);
    }
}
