using System;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Afareet.Editor
{
    /// <summary>
    /// Additional UART-005 Android guard for the roadside-clutter expansion. The primary
    /// street-kit stager already copies every stageable OBJ/MTL/texture companion from the
    /// tracked source root; this gate proves the three clutter Resources imported with usable
    /// UV0, normals and texture-mapped materials before an Android build can proceed.
    /// </summary>
    public sealed class P1ProductionRoadsideClutterBuildGate : IPreprocessBuildWithReport
    {
        private const string ResourceRoot = "Art/TracksEnvironments/CairoStreetKit/Generated";
        private static readonly string[] ResourcePaths =
        {
            ResourceRoot + "/SM_Prop_CairoPlanter_A",
            ResourceRoot + "/SM_Prop_CairoCrateStack_A",
            ResourceRoot + "/SM_Prop_CairoCafeTable_A"
        };

        public int callbackOrder => -125;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report == null || report.summary.platform != BuildTarget.Android)
                return;
            if (AfareetBuildContext.IsDedicatedExperimentalAndroidBuild(report))
            {
                Debug.LogWarning("AFAREET_UART005_ROADSIDE_CLUTTER_EXPERIMENTAL_GATE_BYPASS productionEvidence=false");
                return;
            }

            try
            {
                P1ProductionWorldAssetStager.StageTrackedSourcesOrThrow();
                foreach (var resourcePath in ResourcePaths)
                    ValidateImportedResourceOrThrow(resourcePath);

                Debug.Log(
                    "AFAREET_UART005_ROADSIDE_CLUTTER_GATE_OK resources=3 " +
                    "uv0=true normals=true texturedMaterials=true provenance=tracked-source-stage");
            }
            catch (Exception ex)
            {
                Debug.LogError($"AFAREET_UART005_ROADSIDE_CLUTTER_GATE_BLOCKED reason={ex.Message}");
                throw new BuildFailedException(
                    $"UART-005 roadside clutter Android gate blocked the build: {ex.Message}");
            }
        }

        private static void ValidateImportedResourceOrThrow(string resourcePath)
        {
            var root = Resources.Load<GameObject>(resourcePath);
            if (root == null)
                throw new InvalidOperationException($"roadside clutter Resource missing after staging: {resourcePath}");

            var filters = root.GetComponentsInChildren<MeshFilter>(true);
            if (filters == null || filters.Length == 0)
                throw new InvalidOperationException($"roadside clutter Resource has no imported mesh: {resourcePath}");

            foreach (var filter in filters)
            {
                if (filter == null || filter.sharedMesh == null)
                    throw new InvalidOperationException($"roadside clutter Resource contains a missing mesh: {resourcePath}");

                var mesh = filter.sharedMesh;
                if (mesh.vertexCount <= 0)
                    throw new InvalidOperationException($"roadside clutter imported mesh is empty: {resourcePath}");
                if (mesh.uv == null || mesh.uv.Length != mesh.vertexCount)
                    throw new InvalidOperationException($"roadside clutter Unity import has no complete UV0: {resourcePath}");
                if (mesh.normals == null || mesh.normals.Length != mesh.vertexCount)
                    throw new InvalidOperationException($"roadside clutter Unity import has no complete normals: {resourcePath}");
            }

            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers == null || renderers.Length == 0)
                throw new InvalidOperationException($"roadside clutter Resource has no MeshRenderer: {resourcePath}");

            foreach (var renderer in renderers)
            {
                var textureMapped = false;
                foreach (var material in renderer.sharedMaterials ?? Array.Empty<Material>())
                {
                    if (material != null && material.mainTexture != null)
                    {
                        textureMapped = true;
                        break;
                    }
                }

                if (!textureMapped)
                    throw new InvalidOperationException(
                        $"roadside clutter renderer has no texture-mapped material: {resourcePath}/{renderer.name}");
            }
        }
    }
}
