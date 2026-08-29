using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Afareet.Editor
{
    /// <summary>
    /// Android hard gate for the UART-005 repeated road/curb mobile LOD extension.
    /// Requires distinct tracked OBJ-backed LOD0/LOD1/LOD2 imports with complete surfaces.
    /// </summary>
    public sealed class P1ProductionRoadCurbMobileLodBuildGate : IPreprocessBuildWithReport
    {
        private const string ResourceRoot = "Art/TracksEnvironments/CairoStreetKit/Generated";
        private static readonly string[] Bases = { "SM_Track_CairoRoad_A", "SM_Track_CairoCurb_A" };

        public int callbackOrder => -114;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report == null || report.summary.platform != BuildTarget.Android) return;
            try
            {
                P1ProductionWorldAssetStager.StageTrackedSourcesOrThrow();
                foreach (var baseName in Bases) ValidateTriplet(baseName);
                Debug.Log("AFAREET_UART005_ROAD_CURB_MOBILE_LOD_GATE_OK modules=2 distinctLodSources=4 monotonic=true exactSourceSuffix=true sameMeshReuse=false secondaryColliders=false");
            }
            catch (Exception ex)
            {
                Debug.LogError($"AFAREET_UART005_ROAD_CURB_MOBILE_LOD_GATE_BLOCKED reason={ex.Message}");
                throw new BuildFailedException($"UART-005 road/curb mobile LOD gate blocked Android build: {ex.Message}");
            }
        }

        private static void ValidateTriplet(string baseName)
        {
            var roots = new[]
            {
                Resources.Load<GameObject>($"{ResourceRoot}/{baseName}"),
                Resources.Load<GameObject>($"{ResourceRoot}/{baseName}_LOD1"),
                Resources.Load<GameObject>($"{ResourceRoot}/{baseName}_LOD2")
            };
            for (var lod = 0; lod < roots.Length; lod++)
                if (roots[lod] == null) throw new InvalidOperationException($"road/curb mobile LOD Resource missing: {baseName} LOD{lod}");

            var triangles = new int[3];
            var sourcePaths = new string[3];
            var meshesByLevel = new HashSet<Mesh>[3];
            for (var lod = 0; lod < 3; lod++)
            {
                if (lod > 0 && roots[lod].GetComponentsInChildren<Collider>(true).Length > 0)
                    throw new InvalidOperationException($"secondary road/curb LOD must not introduce colliders: {baseName} LOD{lod}");

                var filters = roots[lod].GetComponentsInChildren<MeshFilter>(true);
                if (filters.Length == 0) throw new InvalidOperationException($"road/curb mobile LOD has no imported mesh: {baseName} LOD{lod}");

                var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var meshes = new HashSet<Mesh>();
                foreach (var filter in filters)
                {
                    var mesh = filter == null ? null : filter.sharedMesh;
                    if (mesh == null || mesh.vertexCount <= 0)
                        throw new InvalidOperationException($"road/curb mobile LOD missing mesh: {baseName} LOD{lod}");
                    if (mesh.uv == null || mesh.uv.Length != mesh.vertexCount)
                        throw new InvalidOperationException($"road/curb mobile LOD missing complete UV0: {baseName} LOD{lod}");
                    if (mesh.normals == null || mesh.normals.Length != mesh.vertexCount)
                        throw new InvalidOperationException($"road/curb mobile LOD missing complete normals: {baseName} LOD{lod}");

                    var meshTriangles = 0;
                    for (var sub = 0; sub < mesh.subMeshCount; sub++) meshTriangles += (int)mesh.GetIndexCount(sub) / 3;
                    if (meshTriangles <= 0) throw new InvalidOperationException($"road/curb mobile LOD has no triangles: {baseName} LOD{lod}");
                    triangles[lod] += meshTriangles;

                    var path = AssetDatabase.GetAssetPath(mesh).Replace('\\', '/');
                    if (string.IsNullOrEmpty(path))
                        throw new InvalidOperationException($"road/curb mobile LOD mesh is not backed by a tracked asset: {baseName} LOD{lod}");
                    paths.Add(path);
                    meshes.Add(mesh);
                }

                if (paths.Count != 1)
                    throw new InvalidOperationException($"road/curb mobile LOD must resolve to one source OBJ: {baseName} LOD{lod}");
                foreach (var path in paths) sourcePaths[lod] = path;
                meshesByLevel[lod] = meshes;

                var expectedSuffix = lod == 0 ? $"/{baseName}.obj" : $"/{baseName}_LOD{lod}.obj";
                if (!sourcePaths[lod].EndsWith(expectedSuffix, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"road/curb mobile LOD resolved unexpected source path: {baseName} LOD{lod} path={sourcePaths[lod]}");

                foreach (var renderer in roots[lod].GetComponentsInChildren<MeshRenderer>(true))
                {
                    var textured = false;
                    foreach (var material in renderer.sharedMaterials ?? Array.Empty<Material>())
                        if (material != null && material.mainTexture != null) { textured = true; break; }
                    if (!textured)
                        throw new InvalidOperationException($"road/curb mobile LOD renderer has no texture-mapped material: {baseName} LOD{lod}");
                }
            }

            if (!(triangles[0] > triangles[1] && triangles[1] > triangles[2] && triangles[2] > 0))
                throw new InvalidOperationException($"road/curb mobile LOD topology is not monotonic: {baseName} {triangles[0]}/{triangles[1]}/{triangles[2]}");
            if (string.Equals(sourcePaths[0], sourcePaths[1], StringComparison.OrdinalIgnoreCase) ||
                string.Equals(sourcePaths[0], sourcePaths[2], StringComparison.OrdinalIgnoreCase) ||
                string.Equals(sourcePaths[1], sourcePaths[2], StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"fake same-source road/curb LOD reuse rejected: {baseName}");
            if (Overlaps(meshesByLevel[0], meshesByLevel[1]) || Overlaps(meshesByLevel[0], meshesByLevel[2]) || Overlaps(meshesByLevel[1], meshesByLevel[2]))
                throw new InvalidOperationException($"fake same-mesh road/curb LOD reuse rejected: {baseName}");
        }

        private static bool Overlaps(HashSet<Mesh> left, HashSet<Mesh> right)
        {
            foreach (var mesh in left) if (right.Contains(mesh)) return true;
            return false;
        }
    }
}
