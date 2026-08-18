namespace Afareet.Race
{
    // Minimal compile-only stand-ins for the Unity-owned components referenced by
    // RaceRanking.Capture. The ranking algorithm itself is compiled from the authoritative
    // production source file, not duplicated here.
    public sealed class RacerCheckpointTracker
    {
        public int AcceptedCount { get; set; }
    }

    public sealed class OneLapRaceTracker
    {
        public bool IsFinished { get; set; }
        public int CompletedLaps { get; set; }
        public float FinishTime { get; set; }
    }
}
