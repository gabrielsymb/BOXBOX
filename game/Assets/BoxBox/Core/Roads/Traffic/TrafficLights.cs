namespace BoxBox.Roads.Traffic
{
    public enum LightPhase { Red = 1, Amber = 2, Green = 3 }

    /// <summary>
    /// REDRIVER2 civ_ai.c TrafficLightCycle: a 512-frame (30 Hz, ~17 s) cycle shared by every
    /// junction. North/south approaches are red for the first half; east/west for the second.
    /// Each green lasts 150 frames (5 s) then 50 frames (1.7 s) of amber.
    /// </summary>
    public static class TrafficLights
    {
        public const float FramesPerSecond = 30f;

        public static LightPhase Phase(RoadExit approach, long frame)
        {
            int timeCnt = (int)(frame & 511);
            if (approach == RoadExit.North || approach == RoadExit.South)
            {
                if (timeCnt < 256) return LightPhase.Red;
                timeCnt -= 256;
            }
            else if (timeCnt > 255) return LightPhase.Red;

            if (timeCnt < 150) return LightPhase.Green;
            if (timeCnt >= 200) return LightPhase.Red;
            return LightPhase.Amber;
        }

        public static LightPhase PhaseAt(RoadExit approach, double seconds) =>
            Phase(approach, (long)(seconds * FramesPerSecond));
    }
}
