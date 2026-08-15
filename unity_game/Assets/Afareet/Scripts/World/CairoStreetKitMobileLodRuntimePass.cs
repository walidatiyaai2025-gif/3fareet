using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afareet.World
{
    /// <summary>
    /// Attaches three-level LODGroups to repeated UART-005 authored visual modules after they
    /// appear in the generated Cairo scene. LOD1/LOD2 are distinct tracked OBJ Resources; this
    /// pass never duplicates LOD0 as a fake LOD and never creates Mesh data or primitives.
    /// </summary>
    public sealed class CairoStreetKitMobileLodRuntimePass : MonoBehaviour
    {
        private const string ResourceRoot = "Art/TracksEnvironments/CairoStreetKit/Generated";
        private const float InitialScanDelaySeconds = .9f;
        private const float RescanSeconds = 1.0f;
        private readonly HashSet<int> configuredInstanceIds = new();
        private readonly HashSet<string> missingLogged = new(StringComparer.Ordinal);
        private float nextScanAt;
        private static bool activationLogged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<CairoStreetKitMobileLodRuntimePass>() != null)
                return;

            var host = new GameObject("AFAREET UART005 MOBILE LOD PASS");
            DontDestroyOnLoad(host);
            host.AddComponent<CairoStreetKitMobileLodRuntimePass>();
        }

        private void Awake()
        {
            nextScanAt = Time.unscaledTime + InitialScanDelaySeconds;
        }

        private void Update()
        {
            if (Time.unscaledTime < nextScanAt) return;
            nextScanAt = Time.unscaledTime + RescanSeconds;
            ConfigurePendingInstances();
        }

        private void ConfigurePendingInstances()
        {
            foreach (var target in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (target == null) continue;
                var baseName = ResolveSourceBaseName(target.name);
                if (string.IsNullOrEmpty(baseName)) continue;

                var id = target.gameObject.GetInstanceID();
                if (configuredInstanceIds.Contains(id)) continue;
                if (target.GetComponent<LODGroup>() != null)
                {
                    configuredInstanceIds.Add(id);
                    continue;
                }

                if (TryAttachDistinctLods(target, baseName))
                    configuredInstanceIds.Add(id);
            }
        }

        private bool TryAttachDistinctLods(Transform target, string baseName)
        {
            var lod0Renderers = target.GetComponentsInChildren<MeshRenderer>(true);
            if (lod0Renderers == null || lod0Renderers.Length == 0)
                return false;

            var lod1Path = $"{ResourceRoot}/{baseName}_LOD1";
            var lod2Path = $"{ResourceRoot}/{baseName}_LOD2";
            var lod1Source = Resources.Load<GameObject>(lod1Path);
            var lod2Source = Resources.Load<GameObject>(lod2Path);
            if (lod1Source == null || lod2Source == null)
            {
                Missing(lod1Source == null ? lod1Path : lod2Path);
                return false;
            }

            GameObject lod1 = null;
            GameObject lod2 = null;
            try
            {
                lod1 = UnityEngine.Object.Instantiate(lod1Source, target, false);
                lod2 = UnityEngine.Object.Instantiate(lod2Source, target, false);
                lod1.name = "UART005 DISTINCT LOD1 SOURCE";
                lod2.name = "UART005 DISTINCT LOD2 SOURCE";
                ResetLocalTransform(lod1.transform);
                ResetLocalTransform(lod2.transform);

                var lod1Renderers = lod1.GetComponentsInChildren<MeshRenderer>(true);
                var lod2Renderers = lod2.GetComponentsInChildren<MeshRenderer>(true);
                if (lod1Renderers.Length == 0 || lod2Renderers.Length == 0)
                    throw new InvalidOperationException($"UART-005 mobile LOD Resource has no renderer: {baseName}");

                var lod0Mesh = FirstMesh(lod0Renderers);
                var lod1Mesh = FirstMesh(lod1Renderers);
                var lod2Mesh = FirstMesh(lod2Renderers);
                if (lod0Mesh == null || lod1Mesh == null || lod2Mesh == null)
                    throw new InvalidOperationException($"UART-005 mobile LOD renderer is missing a mesh: {baseName}");
                if (ReferenceEquals(lod0Mesh, lod1Mesh) || ReferenceEquals(lod0Mesh, lod2Mesh) || ReferenceEquals(lod1Mesh, lod2Mesh))
                    throw new InvalidOperationException($"UART-005 fake same-mesh LOD reuse rejected: {baseName}");

                var lod0Triangles = TriangleCount(lod0Renderers);
                var lod1Triangles = TriangleCount(lod1Renderers);
                var lod2Triangles = TriangleCount(lod2Renderers);
                if (!(lod0Triangles > lod1Triangles && lod1Triangles > lod2Triangles && lod2Triangles > 0))
                    throw new InvalidOperationException(
                        $"UART-005 non-monotonic runtime LOD topology rejected: {baseName} " +
                        $"triangles={lod0Triangles}/{lod1Triangles}/{lod2Triangles}");

                var group = target.gameObject.AddComponent<LODGroup>();
                group.fadeMode = LODFadeMode.CrossFade;
                group.animateCrossFading = false;
                group.SetLODs(new[]
                {
                    new LOD(.56f, lod0Renderers),
                    new LOD(.27f, lod1Renderers),
                    new LOD(.08f, lod2Renderers)
                });
                group.RecalculateBounds();

                if (!activationLogged)
                {
                    activationLogged = true;
                    Debug.Log(
                        "AFAREET_UART005_MOBILE_LOD_ACTIVE levels=3 distinctSources=true " +
                        "transitions=0.56/0.27/0.08 sameMeshReuse=false generatedMesh=false");
                }
                return true;
            }
            catch (Exception ex)
            {
                if (lod1 != null) UnityEngine.Object.Destroy(lod1);
                if (lod2 != null) UnityEngine.Object.Destroy(lod2);
                Debug.LogError($"AFAREET_UART005_MOBILE_LOD_BLOCKED source={baseName} reason={ex.Message}");
                return false;
            }
        }

        private static string ResolveSourceBaseName(string objectName)
        {
            if (objectName.StartsWith("Facade ", StringComparison.Ordinal))
            {
                if (objectName.EndsWith(" V1", StringComparison.Ordinal)) return "SM_Env_CairoFacade_A";
                if (objectName.EndsWith(" V2", StringComparison.Ordinal)) return "SM_Env_CairoFacade_B";
                if (objectName.EndsWith(" V3", StringComparison.Ordinal)) return "SM_Env_CairoFacade_C";
            }

            if (objectName == "Authored Cairo Awning V1") return "SM_Env_CairoAwning_A";
            if (objectName == "Authored Cairo Awning V2") return "SM_Env_CairoAwning_B";
            if (objectName == "AUTHORED CAIRO LAMP") return "SM_Prop_CairoLamp_A";
            if (objectName == "AUTHORED CAIRO BARRIER") return "SM_Prop_CairoBarrier_A";
            if (objectName == "Authored Cairo Hanging Sign") return "SM_Prop_CairoSign_A";
            if (objectName == "Authored Cairo Roadside Clutter V1" || objectName == "Authored Cairo Roadside Planter Secondary")
                return "SM_Prop_CairoPlanter_A";
            if (objectName == "Authored Cairo Roadside Clutter V2") return "SM_Prop_CairoCrateStack_A";
            if (objectName == "Authored Cairo Roadside Clutter V3") return "SM_Prop_CairoCafeTable_A";
            return string.Empty;
        }

        private void Missing(string path)
        {
            if (!missingLogged.Add(path)) return;
            Debug.LogError($"AFAREET_UART005_MOBILE_LOD_RESOURCE_MISSING path={path}");
        }

        private static void ResetLocalTransform(Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        private static Mesh FirstMesh(IEnumerable<MeshRenderer> renderers)
        {
            foreach (var renderer in renderers)
            {
                var filter = renderer == null ? null : renderer.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null)
                    return filter.sharedMesh;
            }
            return null;
        }

        private static int TriangleCount(IEnumerable<MeshRenderer> renderers)
        {
            var total = 0;
            foreach (var renderer in renderers)
            {
                var filter = renderer == null ? null : renderer.GetComponent<MeshFilter>();
                var mesh = filter == null ? null : filter.sharedMesh;
                if (mesh == null) continue;
                for (var sub = 0; sub < mesh.subMeshCount; sub++)
                    total += (int)mesh.GetIndexCount(sub) / 3;
            }
            return total;
        }
    }
}
