using System;

namespace Afareet.Support
{
    public enum MusicPlaybackState { Stopped, Playing, Paused }

    public sealed class MusicLifecycleState
    {
        public MusicPlaybackState State { get; private set; } = MusicPlaybackState.Stopped;
        public string TrackId { get; private set; } = string.Empty;
        public int PlayerGeneration { get; private set; }

        public bool Play(string trackId)
        {
            if (string.IsNullOrWhiteSpace(trackId)) throw new ArgumentException("Track id required.", nameof(trackId));
            var normalized = trackId.Trim();
            if (State == MusicPlaybackState.Playing && TrackId == normalized) return false;
            if (TrackId != normalized) PlayerGeneration++;
            TrackId = normalized;
            State = MusicPlaybackState.Playing;
            return true;
        }

        public bool Pause()
        {
            if (State != MusicPlaybackState.Playing) return false;
            State = MusicPlaybackState.Paused;
            return true;
        }

        public bool Resume()
        {
            if (State != MusicPlaybackState.Paused || TrackId.Length == 0) return false;
            State = MusicPlaybackState.Playing;
            return true;
        }

        public void Stop()
        {
            State = MusicPlaybackState.Stopped;
            TrackId = string.Empty;
        }
    }
}
