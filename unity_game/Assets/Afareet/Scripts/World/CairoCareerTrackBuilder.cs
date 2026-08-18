using System;
using UnityEngine;

namespace Afareet.World
{
    public sealed class CairoCareerTrackBuildResult
    {
        public CairoCareerTrackSpec Spec { get; }
        public GameObject Root { get; }
        public TrackRuntime Track { get; }

        internal CairoCareerTrackBuildResult(
            CairoCareerTrackSpec spec,
            GameObject root,
            TrackRuntime track)
        {
            Spec = spec ?? throw new ArgumentNullException(nameof(spec));
            Root = root != null ? root : throw new ArgumentNullException(nameof(root));
            Track = track ?? throw new ArgumentNullException(nameof(track));
        }
    }

    public static class CairoCareerTrackBuilder
    {
        public static CairoCareerTrackBuildResult Build(Transform parent, string trackId)
        {
            return Build(parent, CairoCareerTrackCatalog.Resolve(trackId));
        }

        public static CairoCareerTrackBuildResult Build(Transform parent, CairoCareerTrackSpec spec)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (spec == null) throw new ArgumentNullException(nameof(spec));

            var root = new GameObject($"CAREER TRACK // {spec.Id}");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.Euler(0f, spec.YawDegrees, 0f);
            root.transform.localScale = new Vector3(spec.ScaleX, 1f, spec.ScaleZ);

            TrackRuntime track;
            try
            {
                // The identity Corniche spec is deliberately the exact existing P1 builder path.
                // Other Career IDs transform the same authoritative authored Cairo layout as a
                // deterministic programming-only variant; no external art is required here.
                track = CairoTrackBuilder.Build(root.transform);
                if (track.Waypoints.Count < 2)
                    throw new InvalidOperationException($"Career track '{spec.Id}' produced fewer than two waypoints.");
            }
            catch
            {
                DestroyCreatedRoot(root);
                throw;
            }

            return new CairoCareerTrackBuildResult(spec, root, track);
        }

        private static void DestroyCreatedRoot(GameObject root)
        {
            if (root == null) return;
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(root);
            else
                UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
