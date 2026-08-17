using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Afareet.Vehicle
{
    public sealed class HeroCarProductionVisual : MonoBehaviour
    {
        public static bool TryAttach(Transform vehicleRoot)
        {
            if (vehicleRoot == null) return false;

            var prefab = Resources.Load<GameObject>(HeroCarLodPolicy.ProductionResourcePath);
            if (prefab == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    $"AFAREET_HERO_AUTHORED_PRODUCTION_MISSING path={HeroCarLodPolicy.ProductionResourcePath} " +
                    "editorFallbackAllowed=true");
#else
                Debug.LogError($"AFAREET_HERO_AUTHORED_PRODUCTION_MISSING path={HeroCarLodPolicy.ProductionResourcePath}");
#endif
                return false;
            }

            if (!ValidateProductionPrefab(prefab, out var reason))
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    $"AFAREET_HERO_AUTHORED_PRODUCTION_REJECTED reason={reason} editorFallbackAllowed=true");
#else
                Debug.LogError($"AFAREET_HERO_AUTHORED_PRODUCTION_REJECTED reason={reason}");
#endif
                return false;
            }

            var metadata = prefab.GetComponent<HeroCarProductionAssetMetadata>();
            var instance = Object.Instantiate(prefab, vehicleRoot, false);
            instance.name = "Hero Authored Production Visual";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            Debug.Log(
                $"AFAREET_HERO_AUTHORED_PRODUCTION_VISUAL_ACTIVE source={metadata.SourceAssetId} " +
                $"path={HeroCarLodPolicy.ProductionResourcePath}");
            return true;
        }

        public static bool TryAttachRefinementCandidate(Transform vehicleRoot)
        {
#if !UNITY_EDITOR && !AFAREET_EXPERIMENTAL_APK
            return false;
#else
            if (vehicleRoot == null) return false;

            var prefab = Resources.Load<GameObject>(HeroCarLodPolicy.RefinementCandidateResourcePath);
            if (prefab == null) return false;

            if (!ValidateRefinementCandidatePrefab(prefab, out var reason))
            {
                Debug.LogWarning($"AFAREET_HERO_REFINEMENT_CANDIDATE_REJECTED reason={reason}");
                return false;
            }

            var marker = prefab.GetComponent<HeroCarRefinementCandidateMarker>();
            var instance = Object.Instantiate(prefab, vehicleRoot, false);
            instance.name = "Hero Afareet King Refinement Candidate";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            Debug.Log(
                $"AFAREET_HERO_REFINEMENT_CANDIDATE_ACTIVE classification={marker.Classification} " +
                $"sourceSha256={marker.SourceSha256} mobileBudgetReady={marker.MobileBudgetReady} " +
                "productionGate=false");
            return true;
#endif
        }

        public static bool TryAttachGeneratedPreview(Transform vehicleRoot)
        {
            if (!Application.isEditor || vehicleRoot == null) return false;

            var prefab = Resources.Load<GameObject>(HeroCarLodPolicy.GeneratedPreviewResourcePath);
            if (prefab == null) return false;

            if (!ValidatePreviewGeometry(prefab, out var reason))
            {
                Debug.LogWarning($"AFAREET_HERO_GENERATED_PREVIEW_REJECTED reason={reason}");
                return false;
            }

            var instance = Object.Instantiate(prefab, vehicleRoot, false);
            instance.name = "Hero Generated Preview V2";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            Debug.Log("AFAREET_HERO_GENERATED_PREVIEW_ACTIVE production=false");
            return true;
        }

        public static bool ValidateProductionPrefab(GameObject prefab, out string reason)
        {
            if (prefab == null)
            {
                reason = "missing-prefab";
                return false;
            }

            var metadata = prefab.GetComponent<HeroCarProductionAssetMetadata>();
            if (metadata == null)
            {
                reason = "missing-production-metadata";
                return false;
            }

            if (!metadata.DeclaresProductionAuthoring)
            {
                reason = "production-metadata-incomplete";
                return false;
            }

            var group = prefab.GetComponent<LODGroup>();
            if (group == null)
            {
                reason = "missing-lod-group";
                return false;
            }

            var lods = group.GetLODs();
            if (lods == null || lods.Length != 3)
            {
                reason = $"lod-count-{(lods == null ? 0 : lods.Length)}";
                return false;
            }

            for (var lod = 0; lod < lods.Length; lod++)
            {
                if (lods[lod].renderers == null || lods[lod].renderers.Length != 1 || lods[lod].renderers[0] == null)
                {
                    reason = $"lod{lod}-renderer-contract";
                    return false;
                }

                var renderer = lods[lod].renderers[0] as MeshRenderer;
                if (renderer == null)
                {
                    reason = $"lod{lod}-mesh-renderer-required";
                    return false;
                }

                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                {
                    reason = $"lod{lod}-missing-mesh";
                    return false;
                }

                var mesh = filter.sharedMesh;
                var triangles = TriangleCount(mesh);
                if (!HeroCarLodPolicy.IsWithinBudget(lod, mesh.vertexCount, triangles))
                {
                    reason = $"lod{lod}-geometry-{mesh.vertexCount}v-{triangles}t";
                    return false;
                }

                var hasUv0 = mesh.HasVertexAttribute(VertexAttribute.TexCoord0);
                var hasNormals = mesh.HasVertexAttribute(VertexAttribute.Normal);
                var hasTextureMappedMaterial = HasTextureMappedMaterial(renderer);

                var productionQuality = HeroCarProductionQualityPolicy.MeetsProductionFloor(
                    lod,
                    triangles,
                    hasUv0 && metadata.Uv0Authored,
                    hasNormals && metadata.NormalsAuthored,
                    hasTextureMappedMaterial && metadata.TextureMappedMaterials);

                if (!productionQuality)
                {
                    reason =
                        $"lod{lod}-production-quality " +
                        $"uv0={hasUv0}/{metadata.Uv0Authored} " +
                        $"normals={hasNormals}/{metadata.NormalsAuthored} " +
                        $"texture={hasTextureMappedMaterial}/{metadata.TextureMappedMaterials}";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        public static bool ValidateRefinementCandidatePrefab(GameObject prefab, out string reason)
        {
            if (prefab == null)
            {
                reason = "missing-refinement-prefab";
                return false;
            }

            if (prefab.GetComponent<HeroCarProductionAssetMetadata>() != null)
            {
                reason = "refinement-carries-production-metadata";
                return false;
            }

            var marker = prefab.GetComponent<HeroCarRefinementCandidateMarker>();
            if (marker == null ||
                marker.Classification != HeroCarRefinementCandidateMarker.ExpectedClassification ||
                marker.CanSatisfyProductionGate)
            {
                reason = "invalid-refinement-classification";
                return false;
            }

            var group = prefab.GetComponent<LODGroup>();
            if (group == null)
            {
                reason = "missing-refinement-lod-group";
                return false;
            }

            var allGroups = prefab.GetComponentsInChildren<LODGroup>(true);
            if (allGroups == null || allGroups.Length != 1 || allGroups[0] != group)
            {
                reason = $"refinement-lod-group-authority-{(allGroups == null ? 0 : allGroups.Length)}";
                return false;
            }

            var lods = group.GetLODs();
            if (lods == null || lods.Length != 3)
            {
                reason = $"refinement-lod-count-{(lods == null ? 0 : lods.Length)}";
                return false;
            }

            var assignedRenderers = new HashSet<Renderer>();
            for (var lod = 0; lod < lods.Length; lod++)
            {
                if (lods[lod].renderers == null || lods[lod].renderers.Length == 0)
                {
                    reason = $"refinement-lod{lod}-missing-renderers";
                    return false;
                }

                foreach (var renderer in lods[lod].renderers)
                {
                    if (renderer == null)
                    {
                        reason = $"refinement-lod{lod}-null-renderer";
                        return false;
                    }

                    if (!assignedRenderers.Add(renderer))
                    {
                        reason = $"refinement-renderer-registered-more-than-once-{renderer.gameObject.name}";
                        return false;
                    }

                    var filter = renderer.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null)
                    {
                        reason = $"refinement-lod{lod}-missing-mesh";
                        return false;
                    }
                }
            }

            reason = string.Empty;
            return true;
        }

        private static bool ValidatePreviewGeometry(GameObject prefab, out string reason)
        {
            if (prefab.GetComponent<HeroCarProductionAssetMetadata>() != null)
            {
                reason = "preview-carries-production-metadata";
                return false;
            }

            var group = prefab.GetComponent<LODGroup>();
            if (group == null)
            {
                reason = "missing-lod-group";
                return false;
            }

            var lods = group.GetLODs();
            if (lods == null || lods.Length != 3)
            {
                reason = $"lod-count-{(lods == null ? 0 : lods.Length)}";
                return false;
            }

            for (var lod = 0; lod < lods.Length; lod++)
            {
                if (lods[lod].renderers == null || lods[lod].renderers.Length != 1 || lods[lod].renderers[0] == null)
                {
                    reason = $"lod{lod}-renderer-contract";
                    return false;
                }

                var filter = lods[lod].renderers[0].GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                {
                    reason = $"lod{lod}-missing-mesh";
                    return false;
                }

                var mesh = filter.sharedMesh;
                var triangles = TriangleCount(mesh);
                if (!HeroCarLodPolicy.IsWithinBudget(lod, mesh.vertexCount, triangles))
                {
                    reason = $"lod{lod}-geometry-{mesh.vertexCount}v-{triangles}t";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        private static bool HasTextureMappedMaterial(Renderer renderer)
        {
            if (renderer.sharedMaterials == null || renderer.sharedMaterials.Length == 0) return false;

            foreach (var material in renderer.sharedMaterials)
            {
                if (material == null || material.shader == null) continue;
                foreach (var propertyName in material.GetTexturePropertyNames())
                {
                    if (material.GetTexture(propertyName) != null)
                        return true;
                }
            }

            return false;
        }

        private static int TriangleCount(Mesh mesh)
        {
            var count = 0;
            for (var sub = 0; sub < mesh.subMeshCount; sub++)
                count += (int)mesh.GetIndexCount(sub) / 3;
            return count;
        }
    }
}
