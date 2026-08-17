using System;
using Afareet.Vehicle;
using UnityEditor;
using UnityEngine;

namespace Afareet.Editor
{
    /// <summary>
    /// Normalizes only importer settings for the three tracked UART-004 OBJ sources.
    /// This does not create geometry or production prefabs. It asks Unity's ModelImporter
    /// to preserve the authored object hierarchy and source vertex/material data before
    /// the read-only UART-004 source preflight evaluates imported Mesh sub-assets.
    /// </summary>
    public static class RivalProductionImportNormalizer
    {
        private const string MenuPath = "Afareet/P1/Rivals/Normalize UART-004 Imports";

        private static readonly string[] SourcePaths =
        {
            "Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_01_WedgeCoupe.obj",
            "Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_02_FastbackMuscle.obj",
            "Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_03_CompactPrototype.obj"
        };

        [MenuItem(MenuPath)]
        public static void NormalizeCurrentSourcesOrThrow()
        {
            RivalProductionPolicy.ValidateContract();
            if (SourcePaths.Length != RivalProductionPolicy.VariantCount)
                throw new InvalidOperationException("UART-004 import normalizer must define exactly three tracked rival sources.");

            for (var variant = 0; variant < SourcePaths.Length; variant++)
                NormalizeSourceOrThrow(variant, SourcePaths[variant]);

            Debug.Log(
                "AFAREET_UART004_IMPORT_NORMALIZE_ALL_OK variants=3 preserveHierarchy=true " +
                "optimizeMeshPolygons=false optimizeMeshVertices=false weldVertices=false " +
                "normals=import materials=standard geometryGenerated=false productionPromotion=false");
        }

        private static void NormalizeSourceOrThrow(int variant, string sourcePath)
        {
            var importer = AssetImporter.GetAtPath(sourcePath) as ModelImporter;
            if (importer == null)
                throw new InvalidOperationException(
                    $"UART-004 source is not handled by ModelImporter: variant={variant + 1} source={sourcePath}");

            var changed = false;
            changed |= Set(ref importer.preserveHierarchy, true);
            changed |= Set(ref importer.optimizeMeshPolygons, false);
            changed |= Set(ref importer.optimizeMeshVertices, false);
            changed |= Set(ref importer.weldVertices, false);
            changed |= Set(ref importer.importAnimation, false);
            changed |= Set(ref importer.importCameras, false);
            changed |= Set(ref importer.importLights, false);

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
                "normals=import materials=standard geometryGenerated=false productionPromotion=false");
        }

        private static bool Set(ref bool current, bool desired)
        {
            if (current == desired) return false;
            current = desired;
            return true;
        }
    }
}
