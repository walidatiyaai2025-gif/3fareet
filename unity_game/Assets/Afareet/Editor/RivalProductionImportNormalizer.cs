using System;
using Afareet.Vehicle;
using UnityEditor;
using UnityEngine;

namespace Afareet.Editor
{
    /// <summary>
    /// Normalizes only importer settings for the three isolated UART-004 production OBJ exchanges.
    /// Historical authored-review sources are deliberately outside RivalProductionPolicy.ProductionSourceRoot
    /// and are not touched by this production normalizer. This does not create geometry or prefabs.
    /// </summary>
    public static class RivalProductionImportNormalizer
    {
        private const string MenuPath = "Afareet/P1/Rivals/Normalize UART-004 Production Imports";

        [MenuItem(MenuPath)]
        public static void NormalizeCurrentSourcesOrThrow()
        {
            RivalProductionPolicy.ValidateContract();

            for (var variant = 0; variant < RivalProductionPolicy.VariantCount; variant++)
                NormalizeSourceOrThrow(variant, RivalProductionPolicy.StagingSourcePath(variant));

            Debug.Log(
                "AFAREET_UART004_IMPORT_NORMALIZE_ALL_OK variants=3 preserveHierarchy=true " +
                $"sourceRoot={RivalProductionPolicy.ProductionSourceRoot} reviewSourcesRejected=true " +
                "optimizeMeshPolygons=false optimizeMeshVertices=false weldVertices=false " +
                "normals=import materials=standard geometryGenerated=false productionPromotion=false");
        }

        private static void NormalizeSourceOrThrow(int variant, string sourcePath)
        {
            if (!RivalProductionPolicy.IsSupportedAuthoredModelSource(sourcePath))
                throw new InvalidOperationException(
                    $"UART-004 import normalizer rejected non-production model path: variant={variant + 1} source={sourcePath}");

            var importer = AssetImporter.GetAtPath(sourcePath) as ModelImporter;
            if (importer == null)
                throw new InvalidOperationException(
                    $"UART-004 production source is not handled by ModelImporter: variant={variant + 1} source={sourcePath}");

            var changed = false;
            if (!importer.preserveHierarchy) { importer.preserveHierarchy = true; changed = true; }
            if (importer.optimizeMeshPolygons) { importer.optimizeMeshPolygons = false; changed = true; }
            if (importer.optimizeMeshVertices) { importer.optimizeMeshVertices = false; changed = true; }
            if (importer.weldVertices) { importer.weldVertices = false; changed = true; }
            if (importer.importAnimation) { importer.importAnimation = false; changed = true; }
            if (importer.importCameras) { importer.importCameras = false; changed = true; }
            if (importer.importLights) { importer.importLights = false; changed = true; }

            if (importer.importNormals != ModelImporterNormals.Import)
            {
                importer.importNormals = ModelImporterNormals.Import;
                changed = true;
            }
            if (importer.materialImportMode != ModelImporterMaterialImportMode.ImportStandard)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                changed = true;
            }

            if (changed)
                importer.SaveAndReimport();
            else
                AssetDatabase.ImportAsset(sourcePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            var refreshed = AssetImporter.GetAtPath(sourcePath) as ModelImporter;
            if (refreshed == null ||
                !refreshed.preserveHierarchy ||
                refreshed.optimizeMeshPolygons ||
                refreshed.optimizeMeshVertices ||
                refreshed.weldVertices ||
                refreshed.importNormals != ModelImporterNormals.Import ||
                refreshed.materialImportMode != ModelImporterMaterialImportMode.ImportStandard)
            {
                throw new InvalidOperationException(
                    $"UART-004 deterministic importer contract did not persist: variant={variant + 1} source={sourcePath}");
            }

            Debug.Log(
                $"AFAREET_UART004_IMPORT_NORMALIZE_OK variant={variant + 1} source={sourcePath} changed={changed} " +
                "preserveHierarchy=true optimizeMeshPolygons=false optimizeMeshVertices=false weldVertices=false " +
                "normals=import materials=standard reviewSourcesRejected=true geometryGenerated=false productionPromotion=false");

            LogImportedTopology(variant, sourcePath);
        }

        private static void LogImportedTopology(int variant, string sourcePath)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            var rendererCount = 0;
            if (model != null)
            {
                foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                {
                    if (rendererCount >= 8) break;
                    var mesh = RivalProductionPolicy.MeshFor(renderer);
                    Debug.Log(
                        $"AFAREET_UART004_IMPORT_RENDERER variant={variant + 1} index={rendererCount} " +
                        $"renderer={renderer.name} mesh={(mesh == null ? "<null>" : mesh.name)} " +
                        $"triangles={RivalImportedLodResolver.TriangleCount(mesh)} production=false");
                    rendererCount++;
                }
            }

            var meshCount = 0;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(sourcePath))
            {
                if (!(asset is Mesh mesh)) continue;
                Debug.Log(
                    $"AFAREET_UART004_IMPORT_MESH variant={variant + 1} index={meshCount} " +
                    $"name={mesh.name} triangles={RivalImportedLodResolver.TriangleCount(mesh)} " +
                    $"subMeshes={mesh.subMeshCount} production=false");
                meshCount++;
                if (meshCount >= 8) break;
            }

            Debug.Log(
                $"AFAREET_UART004_IMPORT_TOPOLOGY variant={variant + 1} renderers={rendererCount} " +
                $"meshSubAssets={meshCount} preserveHierarchy=true production=false");
        }
    }
}