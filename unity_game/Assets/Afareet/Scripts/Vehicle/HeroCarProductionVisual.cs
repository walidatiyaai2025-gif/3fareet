using UnityEngine;

namespace Afareet.Vehicle
{
    public sealed class HeroCarProductionVisual : MonoBehaviour
    {
        public static bool TryAttach(Transform vehicleRoot)
        {
            if (vehicleRoot == null) return false;

            var prefab = Resources.Load<GameObject>(HeroCarLodPolicy.ResourcePath);
            if (prefab == null)
            {
                Debug.LogError($"AFAREET_HERO_PRODUCTION_VISUAL_MISSING path={HeroCarLodPolicy.ResourcePath}");
                return false;
            }

            var instance = Object.Instantiate(prefab, vehicleRoot, false);
            instance.name = "Hero Production Visual";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            if (!ValidateProductionGeometry(instance, out var reason))
            {
                Object.Destroy(instance);
                Debug.LogError($"AFAREET_HERO_PRODUCTION_VISUAL_REJECTED reason={reason}");
                return false;
            }

            Debug.Log("AFAREET_HERO_PRODUCTION_VISUAL_V2_ACTIVE");
            return true;
        }

        private static bool ValidateProductionGeometry(GameObject instance, out string reason)
        {
            var group = instance.GetComponent<LODGroup>();
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
                var triangles = 0;
                for (var sub = 0; sub < mesh.subMeshCount; sub++)
                    triangles += (int)mesh.GetIndexCount(sub) / 3;

                if (!HeroCarLodPolicy.IsWithinBudget(lod, mesh.vertexCount, triangles))
                {
                    reason = $"lod{lod}-geometry-{mesh.vertexCount}v-{triangles}t";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }
    }
}
