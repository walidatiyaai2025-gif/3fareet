using System;

namespace Afareet.CareerRuntime
{
    public interface ICareerTrackRuntime
    {
        string ActiveTrackId { get; }
        bool ApplyTrack(string trackId, bool forceRebuild = false);
    }

    public sealed class PassiveCareerTrackRuntime : ICareerTrackRuntime
    {
        public string ActiveTrackId { get; private set; }

        public PassiveCareerTrackRuntime(string initialTrackId = null)
        {
            if (initialTrackId != null && string.IsNullOrWhiteSpace(initialTrackId))
                throw new ArgumentException("Initial track id must be null or non-blank.", nameof(initialTrackId));
            ActiveTrackId = initialTrackId;
        }

        public bool ApplyTrack(string trackId, bool forceRebuild = false)
        {
            if (string.IsNullOrWhiteSpace(trackId))
                throw new ArgumentException("Career track id is required.", nameof(trackId));
            if (StringComparer.Ordinal.Equals(ActiveTrackId, trackId) && !forceRebuild)
                return false;
            ActiveTrackId = trackId;
            // Passive compatibility runtime never claims a live Unity rebuild, even when the
            // caller explicitly requests one. Production orchestration must use the Core runtime.
            return false;
        }
    }
}
