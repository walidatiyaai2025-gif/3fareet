using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afareet.World
{
    /// <summary>
    /// URAC-011 authored racing-line source. The tracked control points define the Cairo
    /// vertical slice; Catmull-Rom interpolation only densifies that authored line to the
    /// 72 runtime segments expected by the existing race systems.
    /// </summary>
    public static class CairoVerticalSliceLayout
    {
        public const string ResourcePath = "Art/Tracks/CairoVerticalSlice/cairo_vertical_slice_v1";
        public const string AssetPath = "Assets/Afareet/Resources/Art/Tracks/CairoVerticalSlice/cairo_vertical_slice_v1.json";
        public const string LayoutId = "cairo-night-vertical-slice-v1";
        public const int RequiredControlPoints = 24;
        public const int SamplesPerControlPoint = 3;
        public const int RuntimeSegmentCount = RequiredControlPoints * SamplesPerControlPoint;

        [Serializable]
        public sealed class Document
        {
            public int schemaVersion;
            public string layoutId;
            public string authoringState;
            public bool closedLoop;
            public int samplesPerControlPoint;
            public ControlPoint[] points;
        }

        [Serializable]
        public sealed class ControlPoint
        {
            public string id;
            public string sector;
            public Vector3 position;
        }

        public static bool TryLoadSampledPositions(int expectedSegments, out Vector3[] positions, out string reason)
        {
            positions = null;
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                reason = $"missing-layout-resource:{ResourcePath}";
                return false;
            }

            Document document;
            try
            {
                document = JsonUtility.FromJson<Document>(asset.text);
            }
            catch (Exception ex)
            {
                reason = $"layout-json-parse:{ex.GetType().Name}";
                return false;
            }

            if (!ValidateDocument(document, expectedSegments, out reason))
                return false;

            positions = Sample(document);
            if (positions.Length != expectedSegments)
            {
                reason = $"sample-count:{positions.Length}!={expectedSegments}";
                positions = null;
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public static bool ValidateDocument(Document document, int expectedSegments, out string reason)
        {
            if (document == null)
            {
                reason = "null-document";
                return false;
            }
            if (document.schemaVersion != 1)
            {
                reason = $"schema:{document.schemaVersion}";
                return false;
            }
            if (!string.Equals(document.layoutId, LayoutId, StringComparison.Ordinal))
            {
                reason = $"layout-id:{document.layoutId ?? "<null>"}";
                return false;
            }
            if (!string.Equals(document.authoringState, "AUTHORED_LAYOUT", StringComparison.Ordinal))
            {
                reason = $"authoring-state:{document.authoringState ?? "<null>"}";
                return false;
            }
            if (!document.closedLoop)
            {
                reason = "layout-not-closed-loop";
                return false;
            }
            if (document.samplesPerControlPoint != SamplesPerControlPoint)
            {
                reason = $"samples-per-control-point:{document.samplesPerControlPoint}";
                return false;
            }
            if (document.points == null || document.points.Length != RequiredControlPoints)
            {
                reason = $"control-point-count:{(document.points == null ? 0 : document.points.Length)}";
                return false;
            }
            if (expectedSegments != RuntimeSegmentCount)
            {
                reason = $"runtime-segment-contract:{expectedSegments}!={RuntimeSegmentCount}";
                return false;
            }

            var sectors = new HashSet<string>(StringComparer.Ordinal);
            var minX = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;
            var minZ = float.PositiveInfinity;
            var maxZ = float.NegativeInfinity;
            var controlLoopLength = 0f;

            for (var i = 0; i < document.points.Length; i++)
            {
                var point = document.points[i];
                if (point == null || string.IsNullOrWhiteSpace(point.id) || string.IsNullOrWhiteSpace(point.sector))
                {
                    reason = $"control-point-metadata:{i}";
                    return false;
                }

                sectors.Add(point.sector);
                minX = Mathf.Min(minX, point.position.x);
                maxX = Mathf.Max(maxX, point.position.x);
                minZ = Mathf.Min(minZ, point.position.z);
                maxZ = Mathf.Max(maxZ, point.position.z);

                var next = document.points[(i + 1) % document.points.Length];
                if (next == null)
                {
                    reason = $"null-control-point:{(i + 1) % document.points.Length}";
                    return false;
                }

                var gap = Vector3.Distance(point.position, next.position);
                if (gap < 8f)
                {
                    reason = $"control-point-gap:{i}:{gap:F2}";
                    return false;
                }
                controlLoopLength += gap;
            }

            if (sectors.Count < 6)
            {
                reason = $"sector-variety:{sectors.Count}";
                return false;
            }
            if (maxX - minX < 160f || maxZ - minZ < 90f)
            {
                reason = $"layout-extents:{maxX - minX:F1}x{maxZ - minZ:F1}";
                return false;
            }
            if (controlLoopLength < 450f)
            {
                reason = $"layout-length:{controlLoopLength:F1}";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private static Vector3[] Sample(Document document)
        {
            var result = new Vector3[RuntimeSegmentCount];
            var output = 0;
            var points = document.points;

            for (var i = 0; i < points.Length; i++)
            {
                var p0 = points[(i - 1 + points.Length) % points.Length].position;
                var p1 = points[i].position;
                var p2 = points[(i + 1) % points.Length].position;
                var p3 = points[(i + 2) % points.Length].position;

                for (var sample = 0; sample < SamplesPerControlPoint; sample++)
                {
                    var t = sample / (float)SamplesPerControlPoint;
                    result[output++] = CatmullRom(p0, p1, p2, p3, t);
                }
            }

            return result;
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            var t2 = t * t;
            var t3 = t2 * t;
            return .5f * (
                2f * p1 +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }
    }
}
