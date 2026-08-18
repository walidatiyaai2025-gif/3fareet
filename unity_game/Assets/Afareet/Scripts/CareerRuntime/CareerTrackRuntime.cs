using System;

namespace Afareet.CareerRuntime
{
    public interface ICareerTrackRuntime
    {
        string ActiveTrackId { get; }
        bool ApplyTrack(string trackId);
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

        public bool ApplyTrack(string trackId)
        {
            if (string.IsNullOrWhiteSpace(trackId))
                throw new ArgumentException("Career track id is required.", nameof(trackId));
            if (StringComparer.Ordinal.Equals(ActiveTrackId, trackId))
                return false;
            ActiveTrackId = trackId;
            return false;
        }
    }
}
