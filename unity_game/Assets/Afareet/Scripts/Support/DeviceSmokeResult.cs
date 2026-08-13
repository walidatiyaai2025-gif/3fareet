namespace Afareet.Support
{
    public readonly struct DeviceSmokeResult
    {
        public DeviceSmokeResult(bool launch, bool raceStart, bool controls, bool twoMinuteRun)
        { Launch = launch; RaceStart = raceStart; Controls = controls; TwoMinuteRun = twoMinuteRun; }
        public bool Launch { get; }
        public bool RaceStart { get; }
        public bool Controls { get; }
        public bool TwoMinuteRun { get; }
        public bool Passed => Launch && RaceStart && Controls && TwoMinuteRun;
    }
}
