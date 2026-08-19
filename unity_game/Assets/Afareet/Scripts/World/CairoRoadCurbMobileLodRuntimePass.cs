using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Afareet.World
{
    /// <summary>
    /// Extends UART-005 mobile LOD coverage to the repeated authored road and curb renderers.
    /// LOD1/LOD2 are distinct tracked OBJ Resources; no generated Mesh or primitive fallback is allowed.
    /// </summary>
    public sealed class CairoRoadCurbMobileLodRuntimePass : MonoBehaviour
    {
        private const string ResourceRoot = "Art/TracksEnvironments/CairoStreetKit/Generated";
        private const float InitialDelaySeconds = 1.05f;
        private const float RescanSeconds = 1.0f;
        private const float RetrySeconds = 5.0f;

        private readonly HashSet<GameObject> configured = new();
        private readonly Dictionary<GameObject, float> retryAfter = new();
        private readonly Dictionary<string, GameObject> cache = new(StringComparer.Ordinal);
        private readonly HashSet<string> loggedFailures = new(StringComparer.Ordinal);
        private float nextScanAt;
        private static bool activationLogged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<CairoRoadCurbMobileLodRuntimePass>() != null) return;
            var host = new GameObject("AFAREET UART005 ROAD CURB MOBILE LOD PASS");
            DontDestroyOnLoad(host);
            host.AddComponent<CairoRoadCurbMobileLodRuntimePass>();
        }

        private void Awake() => nextScanAt = Time.unscaledTime + InitialDelaySeconds;

        private void Update()
        {
            if (Time.unscaledTime < nextScanAt) return;
            nextScanAt = Time.unscaledTime + RescanSeconds;

            foreach (var target in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (target == null) continue;
                var baseName = ResolveBaseName(target.name);
                if (string.IsNullOrEmpty(baseName)) continue;

                var targetObject = target.gameObject;
                if (configured.Contains(targetObject)) continue;
                if (retryAfter.TryGetValue(targetObject, out var retryAt) && Time.unscaledTime < retryAt) continue;

                if (TryConfigure(target, baseName))
                {
                    configured.Add(targetObject);
                    retryAfter.Remove(targetObject);
                }
                else
                {
                    retryAfter[targetObject] = Time.unscaledTime + RetrySeconds;
                }
            }
        }

        private bool TryConfigure(Transform target, string baseName)
        {
            try
            {
                var existing = target.GetComponent<LODGroup>();
                if (existing != null)
                {
                    ValidateGroup(existing, baseName);
                    return true;
                }

                var lod0 = target.GetComponentsInChildren<MeshRenderer>(true);
                ValidateRenderers(lod0, baseName, 0);

                var lod1Source = Load($"{ResourceRoot}/{baseName}_LOD1");
                var lod2Source = Load($"{ResourceRoot}/{baseName}_LOD2");
                if (lod1Source == null || lod2Source == null)
                    throw new InvalidOperationException($"missing distinct Resource for {baseName}");

                var lod1 = Instantiate(lod1Source, target, false);
                var lod2 = Instantiate(lod2Source, target, false);
                lod1.name = "UART005 ROAD CURB DISTINCT LOD1 SOURCE";
                lod2.name = "UART005 ROAD CURB DISTINCT LOD2 SOURCE";
                ResetLocal(lod1.transform);
                ResetLocal(lod2.transform);

                try
                {
                    RejectColliders(lod1, baseName, 1);
                    RejectColliders(lod2, baseName, 2);
                    var r1 = lod1.GetComponentsInChildren<MeshRenderer>(true);
                    var r2 = lod2.GetComponentsInChildren<MeshRenderer>(true);
                    ValidateTriplet(lod0, r1, r2, baseName);

                    var group = target.gameObject.AddComponent<LODGroup>();
                    group.fadeMode = LODFadeMode.CrossFade;
                    group.animateCrossFading = false;
                    group.SetLODs(new[]
                    {
                        new LOD(.56f, lod0),
                        new LOD(.27f, r1),
                        new LOD(.08f, r2)
                    });
                    group.RecalculateBounds();

                    if (!activationLogged)
                    {
                        activationLogged = true;
                        Debug.Log("AFAREET_UART005_ROAD_CURB_MOBILE_LOD_ACTIVE modules=2 distinctSources=4 transitions=0.56/0.27/0.08 sameMeshReuse=false generatedMesh=false");
                    }
                    return true;
                }
                catch
                {
                    Destroy(lod1);
                    Destroy(lod2);
                    throw;
                }
            }
            catch (Exception ex)
            {
                var key = $"{baseName}:{ex.Message}";
                if (loggedFailures.Add(key))
                    Debug.LogError($"AFAREET_UART005_ROAD_CURB_MOBILE_LOD_BLOCKED source={baseName} reason={ex.Message}");
                return false;
            }
        }

        private GameObject Load(string path)
        {
            if (cache.TryGetValue(path, out var existing) && existing != null) return existing;
            var loaded = Resources.Load<GameObject>(path);
            if (loaded != null) cache[path] = loaded;
            return loaded;
        }

        private static string ResolveBaseName(string name)
        {
            if (name == "Authored Crowned Asphalt") return "SM_Track_CairoRoad_A";
            if (name == "Authored Curb Right" || name == "Authored Curb Left") return "SM_Track_CairoCurb_A";
            return string.Empty;
        }

        private static void ValidateGroup(LODGroup group, string baseName)
        {
            var levels = group.GetLODs();
            if (levels == null || levels.Length != 3)
                throw new InvalidOperationException($"existing road/curb LODGroup must have exactly 3 levels: {baseName}");
            ValidateTriplet(levels[0].renderers, levels[1].renderers, levels[2].renderers, baseName);
        }

        private static void ValidateTriplet(Renderer[] lod0, Renderer[] lod1, Renderer[] lod2, string baseName)
        {
            ValidateRenderers(lod0, baseName, 0);
            ValidateRenderers(lod1, baseName, 1);
            ValidateRenderers(lod2, baseName, 2);

            var m0 = CollectMeshes(lod0);
            var m1 = CollectMeshes(lod1);
            var m2 = CollectMeshes(lod2);
            if (Overlaps(m0, m1) || Overlaps(m0, m2) || Overlaps(m1, m2))
                throw new InvalidOperationException($"fake same-mesh road/curb LOD reuse rejected: {baseName}");

            var t0 = Triangles(lod0);
            var t1 = Triangles(lod1);
            var t2 = Triangles(lod2);
            if (!(t0 > t1 && t1 > t2 && t2 > 0))
                throw new InvalidOperationException($"non-monotonic road/curb LOD topology: {baseName} {t0}/{t1}/{t2}");
        }

        private static void ValidateRenderers(Renderer[] renderers, string baseName, int lod)
        {
            if (renderers == null || renderers.Length == 0)
                throw new InvalidOperationException($"road/curb LOD has no renderer: {baseName} LOD{lod}");

            foreach (var renderer in renderers)
            {
                var filter = renderer == null ? null : renderer.GetComponent<MeshFilter>();
                var mesh = filter == null ? null : filter.sharedMesh;
                if (mesh == null || mesh.vertexCount <= 0)
                    throw new InvalidOperationException($"road/curb LOD has no mesh: {baseName} LOD{lod}");
                // Production runtime meshes are allowed to remain non-readable.
                // Query vertex-buffer metadata rather than CPU-side UV/normal arrays.
                if (!mesh.HasVertexAttribute(VertexAttribute.TexCoord0))
                    throw new InvalidOperationException($"road/curb LOD missing complete UV0: {baseName} LOD{lod}");
                if (!mesh.HasVertexAttribute(VertexAttribute.Normal))
                    throw new InvalidOperationException($"road/curb LOD missing complete normals: {baseName} LOD{lod}");

                // Editor PlayMode may use RuntimeLit preview materials without texture
                // properties. Player runtime keeps imported authored source materials.
                if (!Application.isEditor)
                {
                    var textured = false;
                    foreach (var material in renderer.sharedMaterials ?? Array.Empty<Material>())
                    {
                        if (HasBoundTexture(material))
                        {
                            textured = true;
                            break;
                        }
                    }
                    if (!textured)
                        throw new InvalidOperationException($"road/curb LOD missing texture-mapped material: {baseName} LOD{lod}");
                }
            }
        }

        private static bool HasBoundTexture(Material material)
        {
            if (material == null) return false;
            if (material.HasProperty("_MainTex") && material.GetTexture("_MainTex") != null) return true;
            if (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null) return true;
            return false;
        }

        private static HashSet<Mesh> CollectMeshes(Renderer[] renderers)
        {
            var result = new HashSet<Mesh>();
            foreach (var renderer in renderers)
            {
                var filter = renderer == null ? null : renderer.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null) result.Add(filter.sharedMesh);
            }
            return result;
        }

        private static bool Overlaps(HashSet<Mesh> left, HashSet<Mesh> right)
        {
            foreach (var mesh in left) if (right.Contains(mesh)) return true;
            return false;
        }

        private static int Triangles(Renderer[] renderers)
        {
            var total = 0;
            foreach (var renderer in renderers)
            {
                var filter = renderer == null ? null : renderer.GetComponent<MeshFilter>();
                var mesh = filter == null ? null : filter.sharedMesh;
                if (mesh == null) continue;
                for (var sub = 0; sub < mesh.subMeshCount; sub++) total += (int)mesh.GetIndexCount(sub) / 3;
            }
            return total;
        }

        private static void RejectColliders(GameObject root, string baseName, int lod)
        {
            if (root.GetComponentsInChildren<Collider>(true).Length > 0)
                throw new InvalidOperationException($"secondary road/curb LOD must not introduce colliders: {baseName} LOD{lod}");
        }

        private static void ResetLocal(Transform t)
        {
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }
    }
}
