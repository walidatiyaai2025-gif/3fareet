using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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
        private const float RetryDelaySeconds = 5.0f;
        private const int ExpectedLodLevels = 3;

        private readonly HashSet<Transform> configuredInstances = new();
        private readonly HashSet<string> missingLogged = new(StringComparer.Ordinal);
        private readonly HashSet<string> bindingFailureLogged = new(StringComparer.Ordinal);
        private readonly HashSet<Transform> invalidExistingGroupLogged = new();
        private readonly Dictionary<Transform, float> retryAfterByInstance = new();
        private readonly Dictionary<string, GameObject> resourceCache = new(StringComparer.Ordinal);
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

                if (configuredInstances.Contains(target)) continue;
                if (retryAfterByInstance.TryGetValue(target, out var retryAt) && Time.unscaledTime < retryAt)
                    continue;

                var existingGroup = target.GetComponent<LODGroup>();
                if (existingGroup != null)
                {
                    if (TryValidateExistingGroup(target, baseName, existingGroup))
                    {
                        configuredInstances.Add(target);
                        retryAfterByInstance.Remove(target);
                    }
                    else
                    {
                        retryAfterByInstance[target] = Time.unscaledTime + RetryDelaySeconds;
                    }
                    continue;
                }

                if (TryAttachDistinctLods(target, baseName))
                {
                    configuredInstances.Add(target);
                    retryAfterByInstance.Remove(target);
                }
                else
                {
                    retryAfterByInstance[target] = Time.unscaledTime + RetryDelaySeconds;
                }
            }
        }

        private bool TryAttachDistinctLods(Transform target, string baseName)
        {
            var lod0Renderers = target.GetComponentsInChildren<MeshRenderer>(true);
            if (lod0Renderers == null || lod0Renderers.Length == 0)
                return false;

            var lod1Path = $"{ResourceRoot}/{baseName}_LOD1";
            var lod2Path = $"{ResourceRoot}/{baseName}_LOD2";
            var lod1Source = LoadCached(lod1Path);
            var lod2Source = LoadCached(lod2Path);
            if (lod1Source == null || lod2Source == null)
            {
                Missing(lod1Source == null ? lod1Path : lod2Path);
                return false;
            }

            GameObject lod1 = null;
            GameObject lod2 = null;
            LODGroup group = null;
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
                ValidateRendererSet(lod0Renderers, baseName, 0);
                ValidateRendererSet(lod1Renderers, baseName, 1);
                ValidateRendererSet(lod2Renderers, baseName, 2);
                RejectSecondaryLodColliders(lod1, baseName, 1);
                RejectSecondaryLodColliders(lod2, baseName, 2);

                var lod0Meshes = CollectMeshes(lod0Renderers);
                var lod1Meshes = CollectMeshes(lod1Renderers);
                var lod2Meshes = CollectMeshes(lod2Renderers);
                if (MeshesOverlap(lod0Meshes, lod1Meshes) || MeshesOverlap(lod0Meshes, lod2Meshes) || MeshesOverlap(lod1Meshes, lod2Meshes))
                    throw new InvalidOperationException($"UART-005 fake same-mesh LOD reuse rejected across renderer sets: {baseName}");

                var lod0Triangles = TriangleCount(lod0Renderers);
                var lod1Triangles = TriangleCount(lod1Renderers);
                var lod2Triangles = TriangleCount(lod2Renderers);
                if (!(lod0Triangles > lod1Triangles && lod1Triangles > lod2Triangles && lod2Triangles > 0))
                    throw new InvalidOperationException(
                        $"UART-005 non-monotonic runtime LOD topology rejected: {baseName} " +
                        $"triangles={lod0Triangles}/{lod1Triangles}/{lod2Triangles}");

                group = target.gameObject.AddComponent<LODGroup>();
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
                        "transitions=0.56/0.27/0.08 sameMeshReuse=false generatedMesh=false " +
                        "rendererSurfaceValidation=true retryBackoffSeconds=5 resourceCache=true");
                }
                return true;
            }
            catch (Exception ex)
            {
                if (group != null) UnityEngine.Object.Destroy(group);
                if (lod1 != null) UnityEngine.Object.Destroy(lod1);
                if (lod2 != null) UnityEngine.Object.Destroy(lod2);
                var key = $"{baseName}:{ex.Message}";
                if (bindingFailureLogged.Add(key))
                    Debug.LogError($"AFAREET_UART005_MOBILE_LOD_BLOCKED source={baseName} reason={ex.Message}");
                return false;
            }
        }

        private bool TryValidateExistingGroup(Transform target, string baseName, LODGroup group)
        {
            try
            {
                var levels = group.GetLODs();
                if (levels == null || levels.Length != ExpectedLodLevels)
                    throw new InvalidOperationException($"existing LODGroup must have exactly {ExpectedLodLevels} levels");

                for (var lod = 0; lod < ExpectedLodLevels; lod++)
                    ValidateRendererSet(levels[lod].renderers, baseName, lod);

                var lod0Meshes = CollectMeshes(levels[0].renderers);
                var lod1Meshes = CollectMeshes(levels[1].renderers);
                var lod2Meshes = CollectMeshes(levels[2].renderers);
                if (MeshesOverlap(lod0Meshes, lod1Meshes) || MeshesOverlap(lod0Meshes, lod2Meshes) || MeshesOverlap(lod1Meshes, lod2Meshes))
                    throw new InvalidOperationException("existing LODGroup reuses a Mesh across levels");

                var lod0Triangles = TriangleCount(levels[0].renderers);
                var lod1Triangles = TriangleCount(levels[1].renderers);
                var lod2Triangles = TriangleCount(levels[2].renderers);
                if (!(lod0Triangles > lod1Triangles && lod1Triangles > lod2Triangles && lod2Triangles > 0))
                    throw new InvalidOperationException(
                        $"existing LODGroup topology is not monotonic: {lod0Triangles}/{lod1Triangles}/{lod2Triangles}");

                return true;
            }
            catch (Exception ex)
            {
                if (invalidExistingGroupLogged.Add(target))
                    Debug.LogError($"AFAREET_UART005_MOBILE_LOD_EXISTING_BLOCKED source={baseName} reason={ex.Message}");
                return false;
            }
        }

        private GameObject LoadCached(string path)
        {
            if (resourceCache.TryGetValue(path, out var cached) && cached != null)
                return cached;

            var loaded = Resources.Load<GameObject>(path);
            if (loaded != null)
                resourceCache[path] = loaded;
            return loaded;
        }

        private static void ValidateRendererSet(Renderer[] renderers, string baseName, int lod)
        {
            if (renderers == null || renderers.Length == 0)
                throw new InvalidOperationException($"UART-005 mobile LOD Resource has no renderer: {baseName} LOD{lod}");

            foreach (var renderer in renderers)
            {
                if (renderer == null)
                    throw new InvalidOperationException($"UART-005 mobile LOD contains null renderer: {baseName} LOD{lod}");

                var filter = renderer.GetComponent<MeshFilter>();
                var mesh = filter == null ? null : filter.sharedMesh;
                if (mesh == null)
                    throw new InvalidOperationException($"UART-005 mobile LOD renderer is missing a mesh: {baseName} LOD{lod}");
                if (mesh.vertexCount <= 0)
                    throw new InvalidOperationException($"UART-005 mobile LOD mesh has no vertices: {baseName} LOD{lod}");
                if (!mesh.HasVertexAttribute(VertexAttribute.TexCoord0))
                    throw new InvalidOperationException($"UART-005 mobile LOD missing UV0 vertex attribute: {baseName} LOD{lod}");
                if (!mesh.HasVertexAttribute(VertexAttribute.Normal))
                    throw new InvalidOperationException($"UART-005 mobile LOD missing normal vertex attribute: {baseName} LOD{lod}");

                var meshTriangles = 0;
                for (var sub = 0; sub < mesh.subMeshCount; sub++)
                    meshTriangles += (int)mesh.GetIndexCount(sub) / 3;
                if (meshTriangles <= 0)
                    throw new InvalidOperationException($"UART-005 mobile LOD mesh has no triangles: {baseName} LOD{lod}");

                var textured = false;
                foreach (var material in renderer.sharedMaterials ?? Array.Empty<Material>())
                {
                    if (material != null && material.mainTexture != null)
                    {
                        textured = true;
                        break;
                    }
                }
                if (!textured)
                    throw new InvalidOperationException($"UART-005 mobile LOD renderer has no texture-mapped material: {baseName} LOD{lod}");
            }
        }

        private static HashSet<Mesh> CollectMeshes(Renderer[] renderers)
        {
            var meshes = new HashSet<Mesh>();
            foreach (var renderer in renderers)
            {
                var filter = renderer == null ? null : renderer.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null)
                    meshes.Add(filter.sharedMesh);
            }
            return meshes;
        }

        private static bool MeshesOverlap(HashSet<Mesh> left, HashSet<Mesh> right)
        {
            foreach (var mesh in left)
                if (right.Contains(mesh)) return true;
            return false;
        }

        private static void RejectSecondaryLodColliders(GameObject root, string baseName, int lod)
        {
            if (root.GetComponentsInChildren<Collider>(true).Length > 0)
                throw new InvalidOperationException($"UART-005 secondary mobile LOD must not introduce colliders: {baseName} LOD{lod}");
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

        private static int TriangleCount(Renderer[] renderers)
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
