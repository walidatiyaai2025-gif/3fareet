using System;
using UnityEditor;
using UnityEngine;

namespace Afareet.Editor
{
    public static class CairoStreetKitValidator
    {
        private const string Root = "Assets/Afareet/Art/TracksEnvironments/CairoStreetKit";
        private const string AtlasPath = Root + "/Textures/T_Env_CairoStreetKit_BC.png";
        private const string ShaderPath = Root + "/Shaders/S_Env_CairoStreetAtlas.shader";

        private static readonly string[] PrefabPaths =
        {
            Root + "/Prefabs/PF_Env_CairoFacade_A.prefab",
            Root + "/Prefabs/PF_Env_CairoAwning_A.prefab",
            Root + "/Prefabs/PF_Prop_CairoLamp_A.prefab",
            Root + "/Prefabs/PF_Prop_CairoBarrier_A.prefab"
        };

        private static readonly string[] MaterialPaths =
        {
            Root + "/Materials/M_Env_CairoFacade.mat",
            Root + "/Materials/M_Env_CairoAwning.mat",
            Root + "/Materials/M_Prop_CairoLamp.mat",
            Root + "/Materials/M_Prop_CairoBarrier.mat"
        };

        [MenuItem("Afareet/Validate Cairo Street Kit")]
        public static void ValidateFromMenu()
        {
            ValidateOrThrow();
            Debug.Log("UART-005 Cairo street kit validation passed.");
        }

        public static void ValidateOrThrow()
        {
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
            if (atlas == null)
                throw new InvalidOperationException($"UART-005 atlas failed to import: {AtlasPath}");
            if (atlas.width != 256 || atlas.height != 256)
                throw new InvalidOperationException($"UART-005 atlas must remain 256x256, got {atlas.width}x{atlas.height}.");

            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
                throw new InvalidOperationException($"UART-005 shader failed to import: {ShaderPath}");
            if (shader.name != "Afareet/Environment/CairoStreetAtlas")
                throw new InvalidOperationException($"Unexpected UART-005 shader name: {shader.name}");

            if (MaterialPaths.Length != PrefabPaths.Length)
                throw new InvalidOperationException("UART-005 material/prefab contract is inconsistent.");

            for (var i = 0; i < MaterialPaths.Length; i++)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPaths[i]);
                if (material == null)
                    throw new InvalidOperationException($"UART-005 material failed to import: {MaterialPaths[i]}");
                if (material.shader != shader)
                    throw new InvalidOperationException($"UART-005 material uses the wrong shader: {MaterialPaths[i]}");
                if (material.mainTexture != atlas)
                    throw new InvalidOperationException($"UART-005 material does not use the shared atlas: {MaterialPaths[i]}");

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPaths[i]);
                if (prefab == null)
                    throw new InvalidOperationException($"UART-005 prefab failed to import: {PrefabPaths[i]}");
                if (prefab.transform.localPosition.sqrMagnitude > 0.000001f)
                    throw new InvalidOperationException($"UART-005 prefab root must remain at snap origin: {PrefabPaths[i]}");
                if (prefab.transform.childCount != 1)
                    throw new InvalidOperationException($"UART-005 prefab must contain exactly one geometry child: {PrefabPaths[i]}");

                var child = prefab.transform.GetChild(0);
                var renderer = child.GetComponent<Renderer>();
                var collider = child.GetComponent<BoxCollider>();
                if (renderer == null || renderer.sharedMaterial != material)
                    throw new InvalidOperationException($"UART-005 prefab material binding is invalid: {PrefabPaths[i]}");
                if (collider == null || collider.isTrigger)
                    throw new InvalidOperationException($"UART-005 prefab collider contract is invalid: {PrefabPaths[i]}");
            }
        }
    }
}
