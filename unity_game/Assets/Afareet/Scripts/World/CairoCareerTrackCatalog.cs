using System;
using System.Collections.Generic;

namespace Afareet.World
{
    public sealed class CairoCareerTrackSpec
    {
        public string Id { get; }
        public float ScaleX { get; }
        public float ScaleZ { get; }
        public float YawDegrees { get; }

        public CairoCareerTrackSpec(string id, float scaleX, float scaleZ, float yawDegrees)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Track id is required.", nameof(id));
            if (!IsFinite(scaleX) || scaleX < .65f || scaleX > 1.4f)
                throw new ArgumentOutOfRangeException(nameof(scaleX));
            if (!IsFinite(scaleZ) || scaleZ < .65f || scaleZ > 1.4f)
                throw new ArgumentOutOfRangeException(nameof(scaleZ));
            if (!IsFinite(yawDegrees) || yawDegrees < -180f || yawDegrees > 180f)
                throw new ArgumentOutOfRangeException(nameof(yawDegrees));

            Id = id;
            ScaleX = scaleX;
            ScaleZ = scaleZ;
            YawDegrees = yawDegrees;
        }

        public string DeterministicSignature =>
            $"{Id}|{ScaleX:0.000}|{ScaleZ:0.000}|{YawDegrees:0.000}";

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
                new CairoCareerTrackSpec(CornicheNightId, 1f, 1f, 0f),
                new CairoCareerTrackSpec(KhanSprintId, .82f, 1.08f, 17f),
                new CairoCareerTrackSpec(RingRoadMidnightId, 1.18f, .86f, -11f),
                new CairoCareerTrackSpec(CitadelDriftId, .90f, 1.20f, 29f),
                new CairoCareerTrackSpec(PyramidsSpiritRunId, 1.26f, .98f, -24f)
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
