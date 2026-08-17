using System;
using System.Collections.Generic;
using Afareet.Vehicle;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Afareet.Editor
{
    /// <summary>
    /// Read-only UART-004 source preflight for operator staging. It validates the three
    /// tracked authored rival model imports before any P1 visual-stack stager mutates assets.
    /// This is not owner acceptance, device proof or production promotion.
    /// </summary>
    public static class RivalProductionSourcePreflight
    {
        private static readonly string[] SourcePaths =
        {
            "Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_01_WedgeCoupe.obj",
            "Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_02_FastbackMuscle.obj",
            "Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_03_CompactPrototype.obj"
        };

        internal static void ValidateCurrentSourcesOrThrow()
        {
            RivalProductionPolicy.ValidateContract();
            if (SourcePaths.Length != RivalProductionPolicy.VariantCount)
                throw new InvalidOperationException("UART-004 source preflight must define exactly three tracked rival sources.");

            var uniqueGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var variant = 0; variant < SourcePaths.Length; variant++)
            {
                var guid = ValidateSourceOrThrow(variant, SourcePaths[variant]);
                if (!uniqueGuids.Add(guid))
                    throw new InvalidOperationException(
                        $"UART-004 source preflight rejected duplicate source GUID: variant={variant + 1} guid={guid}");
            }

            Debug.Log(
                "AFAREET_UART004_SOURCE_PREFLIGHT_ALL_OK variants=3 importedSources=3 distinctSources=3 " +
                "lodBands=3 uv0=true normals=true textureMapped=true productionPromotion=false");
        }

        private static string ValidateSourceOrThrow(int variant, string sourcePath)
        {
            if (!RivalProductionPolicy.IsSupportedAuthoredModelSource(sourcePath))
                throw new InvalidOperationException(
                    $"UART-004 source preflight rejected unsupported model path: variant={variant + 1} source={sourcePath}");

            var importer = AssetImporter.GetAtPath(sourcePath);
            var sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (importer == null || sourceModel == null)
                throw new InvalidOperationException(
                    $"UART-004 source preflight missing imported model: variant={variant + 1} source={sourcePath}");

            var guid = AssetDatabase.AssetPathToGUID(sourcePath);
            if (string.IsNullOrWhiteSpace(guid))
                throw new InvalidOperationException(
                    $"UART-004 source preflight could not resolve source GUID: variant={variant + 1} source={sourcePath}");

            var triangles = new int[RivalProductionPolicy.MinimumTriangles.Length];
            var rendererCounts = new int[RivalProductionPolicy.MinimumTriangles.Length];

            foreach (var renderer in sourceModel.GetComponentsInChildren<Renderer>(true))
            {
                var lod = ResolveLod(renderer.transform, sourceModel.transform);
                if (lod < 0) continue;

                var mesh = RivalProductionPolicy.MeshFor(renderer);
                if (mesh == null)
                    throw new InvalidOperationException(
                        $"UART-004 source preflight LOD{lod} renderer has no mesh: variant={variant + 1} renderer={renderer.name}");

                var meshPath = AssetDatabase.GetAssetPath(mesh).Replace('\\', '/');
                if (!string.Equals(meshPath, sourcePath, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        $"UART-004 source preflight refuses non-source mesh: variant={variant + 1} LOD{lod} " +
                        $"renderer={renderer.name} mesh={meshPath} source={sourcePath}");

                if (!mesh.HasVertexAttribute(VertexAttribute.TexCoord0))
                    throw new InvalidOperationException(
                        $"UART-004 source preflight missing UV0: variant={variant + 1} LOD{lod} renderer={renderer.name}");
                if (!mesh.HasVertexAttribute(VertexAttribute.Normal))
                    throw new InvalidOperationException(
                        $"UART-004 source preflight missing authored normals: variant={variant + 1} LOD{lod} renderer={renderer.name}");
                if (!HasAssignedTexture(renderer))
                    throw new InvalidOperationException(
                        $"UART-004 source preflight missing texture-mapped material: variant={variant + 1} LOD{lod} renderer={renderer.name}");

                triangles[lod] += TriangleCount(mesh);
                rendererCounts[lod]++;
            }

            for (var lod = 0; lod < rendererCounts.Length; lod++)
            {
                if (rendererCounts[lod] == 0)
                    throw new InvalidOperationException(
                        $"UART-004 source preflight found no LOD{lod} renderer: variant={variant + 1} " +
                        $"source={sourcePath} expectedObjectSuffix=_LOD{lod}");

                if (!RivalProductionPolicy.MeetsProductionFloor(lod, triangles[lod], true, true, true))
                    throw new InvalidOperationException(
                        $"UART-004 source preflight triangle band failed: variant={variant + 1} LOD{lod} " +
                        $"triangles={triangles[lod]} min={RivalProductionPolicy.MinimumTriangles[lod]} " +
                        $"max={RivalProductionPolicy.MaximumTriangles[lod]}");
            }

            Debug.Log(
                $"AFAREET_UART004_SOURCE_PREFLIGHT_OK variant={variant + 1} source={sourcePath} guid={guid} " +
                $"lod0Triangles={triangles[0]} lod1Triangles={triangles[1]} lod2Triangles={triangles[2]} " +
                "uv0=true normals=true textureMapped=true productionPromotion=false");
            return guid;
        }

        private static int ResolveLod(Transform rendererTransform, Transform importedRoot)
        {
            for (var current = rendererTransform; current != null; current = current.parent)
            {
                for (var lod = 0; lod < RivalProductionPolicy.MinimumTriangles.Length; lod++)
                {
                    if (current.name.EndsWith($"_LOD{lod}", StringComparison.OrdinalIgnoreCase))
                        return lod;
                }

                if (current == importedRoot) break;
            }
            return -1;
        }

        private static bool HasAssignedTexture(Renderer renderer)
        {
            foreach (var material in renderer.sharedMaterials ?? Array.Empty<Material>())
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
