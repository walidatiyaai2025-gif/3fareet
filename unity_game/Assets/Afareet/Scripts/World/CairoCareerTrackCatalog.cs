using System;
using System.Collections.Generic;

namespace Afareet.World
{
    public sealed class CairoCareerTrackSpec
    {
        public string Id { get; }
        public float UniformScale { get; }
        public float YawDegrees { get; }

        public CairoCareerTrackSpec(string id, float uniformScale, float yawDegrees)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Track id is required.", nameof(id));
            if (!IsFinite(uniformScale) || uniformScale < .65f || uniformScale > 1.4f)
                throw new ArgumentOutOfRangeException(nameof(uniformScale));
            if (!IsFinite(yawDegrees) || yawDegrees < -180f || yawDegrees > 180f)
                throw new ArgumentOutOfRangeException(nameof(yawDegrees));

            Id = id;
            UniformScale = uniformScale;
            YawDegrees = yawDegrees;
        }

        public string DeterministicSignature =>
            $"{Id}|{UniformScale:0.000}|{YawDegrees:0.000}";

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public static class CairoCareerTrackCatalog
    {
        public const string CornicheNightId = "cairo_corniche_night";
        public const string KhanSprintId = "khan_el_khalili_sprint";
        public const string RingRoadMidnightId = "ring_road_midnight";
        public const string CitadelDriftId = "citadel_drift";
        public const string PyramidsSpiritRunId = "pyramids_spirit_run";

        private static readonly IReadOnlyList<CairoCareerTrackSpec> specs =
            Array.AsReadOnly(new[]
            {
                // Identity transform intentionally preserves the existing P1 Cairo authored route.
                new CairoCareerTrackSpec(CornicheNightId, 1f, 0f),
                new CairoCareerTrackSpec(KhanSprintId, .86f, 17f),
                new CairoCareerTrackSpec(RingRoadMidnightId, 1.12f, -11f),
                new CairoCareerTrackSpec(CitadelDriftId, .96f, 29f),
                new CairoCareerTrackSpec(PyramidsSpiritRunId, 1.22f, -24f)
            });

        public static IReadOnlyList<CairoCareerTrackSpec> Specs => specs;

        public static CairoCareerTrackSpec Resolve(string trackId)
        {
            if (!TryResolve(trackId, out var spec))
                throw new ArgumentException($"Unknown Cairo Career track id '{trackId ?? "<null>"}'.", nameof(trackId));
            return spec;
        }

        public static bool TryResolve(string trackId, out CairoCareerTrackSpec spec)
        {
            if (string.IsNullOrWhiteSpace(trackId))
            {
                spec = null;
                return false;
            }

            for (var index = 0; index < specs.Count; index++)
            {
                if (StringComparer.Ordinal.Equals(specs[index].Id, trackId))
                {
                    spec = specs[index];
                    return true;
                }
            }

            spec = null;
            return false;
        }
    }
}
