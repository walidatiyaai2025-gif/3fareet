using UnityEngine;

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
                Debug.LogError($"AFAREET_HERO_AUTHORED_PRODUCTION_MISSING path={HeroCarLodPolicy.ProductionResourcePath}");
                return false;
            }

            if (!ValidateProductionPrefab(prefab, out var reason))
            {
                Debug.LogError($"AFAREET_HERO_AUTHORED_PRODUCTION_REJECTED reason={reason}");
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

                var hasUv0 = mesh.uv != null && mesh.uv.Length == mesh.vertexCount;
                var hasNormals = mesh.normals != null && mesh.normals.Length == mesh.vertexCount;
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
                if (material != null && material.mainTexture != null)
                    return true;
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
