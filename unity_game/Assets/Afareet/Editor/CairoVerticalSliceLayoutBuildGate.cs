using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Afareet.Editor
{
    /// <summary>
    /// URAC-011 Android fail-closed guard. The Cairo race line must come from the tracked
    /// authored vertical-slice layout; the historical RadiusX/RadiusZ ellipse is Editor-only.
    /// </summary>
    public sealed class CairoVerticalSliceLayoutBuildGate : IPreprocessBuildWithReport
    {
        private const string AssetPath = "Assets/Afareet/Resources/Art/Tracks/CairoVerticalSlice/cairo_vertical_slice_v1.json";
        private const string LayoutId = "cairo-night-vertical-slice-v1";
        private const int RequiredControlPoints = 24;
        private const int SamplesPerControlPoint = 3;
        private const int RequiredRuntimeSegments = 72;

        [Serializable]
        private sealed class Document
        {
            public int schemaVersion;
            public string layoutId;
            public string authoringState;
            public bool closedLoop;
            public int samplesPerControlPoint;
            public ControlPoint[] points;
        }

        [Serializable]
        private sealed class ControlPoint
        {
            public string id;
            public string sector;
            public Vector3 position;
        }

        public int callbackOrder => -850;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android) return;

            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetPath);
            if (asset == null)
                Fail($"missing-authored-layout path={AssetPath}");

            var document = JsonUtility.FromJson<Document>(asset.text);
            if (document == null)
                Fail("layout-json-parse-failed");
            if (document.schemaVersion != 1)
                Fail($"schema={document.schemaVersion}");
            if (!string.Equals(document.layoutId, LayoutId, StringComparison.Ordinal))
                Fail($"layoutId={document.layoutId ?? "<null>"}");
            if (!string.Equals(document.authoringState, "AUTHORED_LAYOUT", StringComparison.Ordinal))
                Fail($"authoringState={document.authoringState ?? "<null>"}");
            if (!document.closedLoop)
                Fail("closedLoop=false");
            if (document.samplesPerControlPoint != SamplesPerControlPoint)
                Fail($"samplesPerControlPoint={document.samplesPerControlPoint}");
            if (document.points == null || document.points.Length != RequiredControlPoints)
                Fail($"controlPoints={(document.points == null ? 0 : document.points.Length)} expected={RequiredControlPoints}");
            if (document.points.Length * document.samplesPerControlPoint != RequiredRuntimeSegments)
                Fail(
                    $"runtimeSegments={document.points.Length * document.samplesPerControlPoint} " +
                    $"expected={RequiredRuntimeSegments}");

            var sectors = new HashSet<string>(StringComparer.Ordinal);
            var minX = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;
            var minZ = float.PositiveInfinity;
            var maxZ = float.NegativeInfinity;
            var loopLength = 0f;

            for (var i = 0; i < document.points.Length; i++)
            {
                var point = document.points[i];
                if (point == null || string.IsNullOrWhiteSpace(point.id) || string.IsNullOrWhiteSpace(point.sector))
                    Fail($"control-point-metadata={i}");

                sectors.Add(point.sector);
                minX = Mathf.Min(minX, point.position.x);
                maxX = Mathf.Max(maxX, point.position.x);
                minZ = Mathf.Min(minZ, point.position.z);
                maxZ = Mathf.Max(maxZ, point.position.z);

                var next = document.points[(i + 1) % document.points.Length];
                if (next == null)
                    Fail($"null-control-point={(i + 1) % document.points.Length}");

                var gap = Vector3.Distance(point.position, next.position);
                if (gap < 8f)
                    Fail($"control-point-gap index={i} gap={gap:F2}");
                loopLength += gap;
            }

            var width = maxX - minX;
            var depth = maxZ - minZ;
            if (sectors.Count < 6)
                Fail($"sector-variety={sectors.Count}");
            if (width < 160f || depth < 90f)
                Fail($"layout-extents={width:F1}x{depth:F1}");
            if (loopLength < 450f)
                Fail($"layout-length={loopLength:F1}");

            Debug.Log(
                $"AFAREET_URAC011_VERTICAL_SLICE_GATE_OK layout={LayoutId} " +
                $"controlPoints={document.points.Length} sectors={sectors.Count} " +
                $"runtimeSegments={RequiredRuntimeSegments} loopLength={loopLength:F1}");
        }

        private static void Fail(string reason)
        {
            throw new BuildFailedException($"AFAREET_URAC011_VERTICAL_SLICE_GATE_BLOCKED {reason}");
        }
    }
}
