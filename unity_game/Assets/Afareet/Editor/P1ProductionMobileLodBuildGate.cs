using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Afareet.Editor
{
    public sealed class P1ProductionMobileLodBuildGate : IPreprocessBuildWithReport
    {
        private const string ResourceRoot = "Art/TracksEnvironments/CairoStreetKit/Generated";
        private static readonly string[] Bases =
        {
            "SM_Env_CairoFacade_A","SM_Env_CairoFacade_B","SM_Env_CairoFacade_C",
            "SM_Env_CairoAwning_A","SM_Env_CairoAwning_B","SM_Prop_CairoLamp_A",
            "SM_Prop_CairoBarrier_A","SM_Prop_CairoSign_A","SM_Prop_CairoPlanter_A",
            "SM_Prop_CairoCrateStack_A","SM_Prop_CairoCafeTable_A"
        };

        public int callbackOrder => -115;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report == null || report.summary.platform != BuildTarget.Android) return;
            if (AfareetBuildContext.IsDedicatedExperimentalAndroidBuild(report))
            {
                Debug.LogWarning("AFAREET_UART005_MOBILE_LOD_EXPERIMENTAL_GATE_BYPASS productionEvidence=false");
                return;
            }
            try
            {
                P1ProductionWorldAssetStager.StageTrackedSourcesOrThrow();
                foreach (var baseName in Bases) ValidateTriplet(baseName);
                Debug.Log(
                    "AFAREET_UART005_MOBILE_LOD_GATE_OK modules=11 distinctLodSources=22 " +
                    "monotonic=true sameMeshReuse=false exactSourceSuffix=true secondaryColliders=false");
            }
            catch (Exception ex)
            {
                Debug.LogError($"AFAREET_UART005_MOBILE_LOD_GATE_BLOCKED reason={ex.Message}");
                throw new BuildFailedException($"UART-005 mobile LOD gate blocked Android build: {ex.Message}");
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
            for (var i = 0; i < roots.Length; i++)
                if (roots[i] == null) throw new InvalidOperationException($"mobile LOD Resource missing: {baseName} LOD{i}");

            var triangleCounts = new int[3];
            var sourcePaths = new string[3];
            var levelMeshes = new HashSet<Mesh>[3];
            for (var lod = 0; lod < 3; lod++)
            {
                var filters = roots[lod].GetComponentsInChildren<MeshFilter>(true);
                if (filters.Length == 0) throw new InvalidOperationException($"mobile LOD has no imported mesh: {baseName} LOD{lod}");

                if (lod > 0 && roots[lod].GetComponentsInChildren<Collider>(true).Length > 0)
                    throw new InvalidOperationException($"secondary mobile LOD must not introduce colliders: {baseName} LOD{lod}");

                var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var meshes = new HashSet<Mesh>();
                foreach (var filter in filters)
                {
                    var mesh = filter == null ? null : filter.sharedMesh;
                    if (mesh == null) throw new InvalidOperationException($"mobile LOD missing mesh: {baseName} LOD{lod}");
                    if (mesh.vertexCount <= 0) throw new InvalidOperationException($"mobile LOD mesh has no vertices: {baseName} LOD{lod}");
                    if (mesh.uv == null || mesh.uv.Length != mesh.vertexCount) throw new InvalidOperationException($"mobile LOD missing complete UV0: {baseName} LOD{lod}");
                    if (mesh.normals == null || mesh.normals.Length != mesh.vertexCount) throw new InvalidOperationException($"mobile LOD missing complete normals: {baseName} LOD{lod}");

                    var meshTriangles = 0;
                    for (var sub = 0; sub < mesh.subMeshCount; sub++)
                        meshTriangles += (int)mesh.GetIndexCount(sub) / 3;
                    if (meshTriangles <= 0) throw new InvalidOperationException($"mobile LOD mesh has no triangles: {baseName} LOD{lod}");
                    triangleCounts[lod] += meshTriangles;

                    var path = AssetDatabase.GetAssetPath(mesh).Replace('\\','/');
                    if (string.IsNullOrEmpty(path))
                        throw new InvalidOperationException($"mobile LOD mesh is not backed by a tracked asset: {baseName} LOD{lod}");
                    paths.Add(path);
                    meshes.Add(mesh);
                }
                if (paths.Count != 1) throw new InvalidOperationException($"mobile LOD must resolve to one source OBJ: {baseName} LOD{lod}");
                foreach (var p in paths) sourcePaths[lod] = p;
                levelMeshes[lod] = meshes;

                var expectedSuffix = lod == 0 ? $"/{baseName}.obj" : $"/{baseName}_LOD{lod}.obj";
                if (!sourcePaths[lod].EndsWith(expectedSuffix, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        $"mobile LOD resolved unexpected source path: {baseName} LOD{lod} path={sourcePaths[lod]}");

                foreach (var renderer in roots[lod].GetComponentsInChildren<MeshRenderer>(true))
                {
                    var textured = false;
                    foreach (var material in renderer.sharedMaterials ?? Array.Empty<Material>())
                        if (material != null && material.mainTexture != null) { textured = true; break; }
                    if (!textured) throw new InvalidOperationException($"mobile LOD renderer has no texture-mapped material: {baseName} LOD{lod}");
                }
            }

            if (!(triangleCounts[0] > triangleCounts[1] && triangleCounts[1] > triangleCounts[2] && triangleCounts[2] > 0))
                throw new InvalidOperationException($"mobile LOD topology is not monotonic: {baseName} {triangleCounts[0]}/{triangleCounts[1]}/{triangleCounts[2]}");
            if (string.Equals(sourcePaths[0], sourcePaths[1], StringComparison.OrdinalIgnoreCase) ||
                string.Equals(sourcePaths[0], sourcePaths[2], StringComparison.OrdinalIgnoreCase) ||
                string.Equals(sourcePaths[1], sourcePaths[2], StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"fake same-source LOD reuse rejected: {baseName}");
            if (MeshesOverlap(levelMeshes[0], levelMeshes[1]) || MeshesOverlap(levelMeshes[0], levelMeshes[2]) || MeshesOverlap(levelMeshes[1], levelMeshes[2]))
                throw new InvalidOperationException($"fake same-mesh LOD reuse rejected across imported levels: {baseName}");
        }

        private static bool MeshesOverlap(HashSet<Mesh> left, HashSet<Mesh> right)
        {
            foreach (var mesh in left)
                if (right.Contains(mesh)) return true;
            return false;
        }
    }
}
