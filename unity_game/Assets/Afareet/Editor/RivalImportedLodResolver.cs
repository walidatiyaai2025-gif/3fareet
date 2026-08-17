using System;
using System.Collections.Generic;
using System.IO;
using Afareet.Vehicle;
using UnityEditor;
using UnityEngine;

namespace Afareet.Editor
{
    /// <summary>
    /// Unity 6000.5 may flatten OBJ `o` names out of the imported Transform hierarchy.
    /// UART-004 therefore derives an exact LOD/material signature from the tracked OBJ text,
    /// then resolves the already-imported source Mesh sub-assets by authored name or exact
    /// triangle identity. No Mesh data, primitives, replacement geometry or broad quality-band
    /// guesses are created here.
    /// </summary>
    internal static class RivalImportedLodResolver
    {
        internal sealed class SourceSignature
        {
            internal readonly int[] Triangles;
            internal readonly string[] ObjectNames;
            internal readonly string[][] MaterialNames;

            internal SourceSignature(int[] triangles, string[] objectNames, string[][] materialNames)
            {
                Triangles = triangles;
                ObjectNames = objectNames;
                MaterialNames = materialNames;
            }
        }

        internal static SourceSignature ParseSourceOrThrow(string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
                throw new InvalidOperationException($"UART-004 LOD resolver cannot read tracked OBJ source: {sourcePath}");

            var lodCount = RivalProductionPolicy.MinimumTriangles.Length;
            var triangles = new int[lodCount];
            var objectNames = new string[lodCount];
            var seenObjects = new bool[lodCount];
            var materialLists = new List<string>[lodCount];
            for (var lod = 0; lod < lodCount; lod++)
                materialLists[lod] = new List<string>();

            var currentLod = -1;
            foreach (var rawLine in File.ReadLines(sourcePath))
            {
                var line = rawLine.Trim();
                if (line.StartsWith("o ", StringComparison.Ordinal))
                {
                    var objectName = line.Substring(2).Trim();
                    currentLod = ResolveLodFromName(objectName);
                    if (currentLod >= 0)
                    {
                        if (seenObjects[currentLod])
                            throw new InvalidOperationException(
                                $"UART-004 source defines more than one authored object for LOD{currentLod}: source={sourcePath}");
                        seenObjects[currentLod] = true;
                        objectNames[currentLod] = objectName;
                    }
                    continue;
                }

                if (currentLod < 0)
                    continue;

                if (line.StartsWith("usemtl ", StringComparison.Ordinal))
                {
                    var materialName = line.Substring("usemtl ".Length).Trim();
                    if (!string.IsNullOrWhiteSpace(materialName) &&
                        !ContainsOrdinalIgnoreCase(materialLists[currentLod], materialName))
                        materialLists[currentLod].Add(materialName);
                    continue;
                }

                if (!line.StartsWith("f ", StringComparison.Ordinal))
                    continue;

                var fields = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                var vertices = fields.Length - 1;
                if (vertices < 3)
                    throw new InvalidOperationException(
                        $"UART-004 source contains an invalid face in {objectNames[currentLod]}: source={sourcePath}");
                triangles[currentLod] += vertices - 2;
            }

            var materialNames = new string[lodCount][];
            for (var lod = 0; lod < lodCount; lod++)
            {
                if (!seenObjects[lod] || triangles[lod] <= 0)
                    throw new InvalidOperationException(
                        $"UART-004 source triangle signature missing LOD{lod}: source={sourcePath} expectedObjectSuffix=_LOD{lod}");
                if (materialLists[lod].Count == 0)
                    throw new InvalidOperationException(
                        $"UART-004 source material signature missing LOD{lod}: source={sourcePath}");

                for (var other = 0; other < lod; other++)
                {
                    if (triangles[other] == triangles[lod])
                        throw new InvalidOperationException(
                            $"UART-004 source triangle signatures are ambiguous: source={sourcePath} " +
                            $"LOD{other}=LOD{lod}={triangles[lod]} triangles");
                }
                materialNames[lod] = materialLists[lod].ToArray();
            }

            return new SourceSignature(triangles, objectNames, materialNames);
        }

        internal static Mesh[] ResolveImportedMeshesOrThrow(
            string sourcePath,
            GameObject sourceModel,
            SourceSignature signature)
        {
            if (sourceModel == null || signature == null)
                throw new InvalidOperationException($"UART-004 imported mesh resolver received invalid source state: {sourcePath}");

            var lodCount = signature.Triangles.Length;
            var candidates = new List<Mesh>[lodCount];
            for (var lod = 0; lod < lodCount; lod++)
                candidates[lod] = new List<Mesh>();

            foreach (var renderer in sourceModel.GetComponentsInChildren<Renderer>(true))
            {
                var mesh = RivalProductionPolicy.MeshFor(renderer);
                var lod = Resolve(renderer, sourceModel.transform, signature);
                AddCandidate(candidates, lod, mesh, sourcePath);
            }

            // Unity can preserve model Mesh sub-assets even when the Model Prefab hierarchy is
            // flattened. Read them directly instead of requiring a renderer hierarchy shape.
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(sourcePath))
            {
                if (!(asset is Mesh mesh)) continue;
                var lod = Resolve(mesh, signature);
                AddCandidate(candidates, lod, mesh, sourcePath);
            }

            var resolved = new Mesh[lodCount];
            for (var lod = 0; lod < lodCount; lod++)
            {
                if (candidates[lod].Count != 1)
                    throw new InvalidOperationException(
                        $"UART-004 imported source LOD resolution failed: source={sourcePath} LOD{lod} " +
                        $"authoredObject={signature.ObjectNames[lod]} sourceTriangles={signature.Triangles[lod]} " +
                        $"candidateMeshes={candidates[lod].Count} topology={DescribeImportedTopology(sourcePath, sourceModel)}");
                resolved[lod] = candidates[lod][0];
            }
            return resolved;
        }

        internal static Material[][] ResolveImportedMaterialsOrThrow(
            string sourcePath,
            GameObject sourceModel,
            SourceSignature signature)
        {
            var byName = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
            foreach (var renderer in sourceModel.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var material in renderer.sharedMaterials ?? Array.Empty<Material>())
                    AddMaterial(byName, material);
            }
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(sourcePath))
                if (asset is Material material) AddMaterial(byName, material);

            var result = new Material[signature.MaterialNames.Length][];
            for (var lod = 0; lod < signature.MaterialNames.Length; lod++)
            {
                var names = signature.MaterialNames[lod];
                var materials = new Material[names.Length];
                for (var slot = 0; slot < names.Length; slot++)
                {
                    if (!byName.TryGetValue(names[slot], out var material) || material == null)
                        throw new InvalidOperationException(
                            $"UART-004 imported material resolution failed: source={sourcePath} LOD{lod} material={names[slot]} " +
                            $"available={string.Join(",", byName.Keys)}");
                    materials[slot] = material;
                }
                result[lod] = materials;
            }
            return result;
        }

        internal static int Resolve(Renderer renderer, Transform importedRoot, SourceSignature signature)
        {
            if (renderer == null || importedRoot == null || signature == null)
                return -1;

            for (var current = renderer.transform; current != null; current = current.parent)
            {
                var transformLod = ResolveLodFromName(current.name);
                if (transformLod >= 0)
                    return transformLod;
                if (current == importedRoot) break;
            }
            return Resolve(RivalProductionPolicy.MeshFor(renderer), signature);
        }

        internal static int Resolve(Mesh mesh, SourceSignature signature)
        {
            if (mesh == null || signature == null)
                return -1;

            var meshNameLod = ResolveLodFromName(mesh.name);
            if (meshNameLod >= 0)
                return meshNameLod;

            var meshTriangles = TriangleCount(mesh);
            var resolved = -1;
            for (var lod = 0; lod < signature.Triangles.Length; lod++)
            {
                if (meshTriangles != signature.Triangles[lod]) continue;
                if (resolved >= 0) return -1;
                resolved = lod;
            }
            return resolved;
        }

        internal static int ResolveLodFromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return -1;

            for (var lod = 0; lod < RivalProductionPolicy.MinimumTriangles.Length; lod++)
            {
                var token = $"_LOD{lod}";
                var searchIndex = 0;
                while (searchIndex < name.Length)
                {
                    var index = name.IndexOf(token, searchIndex, StringComparison.OrdinalIgnoreCase);
                    if (index < 0) break;
                    var suffixEnd = index + token.Length;
                    if (suffixEnd == name.Length || !char.IsDigit(name[suffixEnd]))
                        return lod;
                    searchIndex = suffixEnd;
                }
            }
            return -1;
        }

        internal static int TriangleCount(Mesh mesh)
        {
            if (mesh == null) return 0;
            var count = 0;
            for (var sub = 0; sub < mesh.subMeshCount; sub++)
                count += (int)mesh.GetIndexCount(sub) / 3;
            return count;
        }

        private static void AddCandidate(List<Mesh>[] candidates, int lod, Mesh mesh, string sourcePath)
        {
            if (lod < 0 || mesh == null) return;
            var meshPath = AssetDatabase.GetAssetPath(mesh).Replace('\\', '/');
            if (!string.Equals(meshPath, sourcePath, StringComparison.Ordinal)) return;
            if (!candidates[lod].Contains(mesh)) candidates[lod].Add(mesh);
        }

        private static void AddMaterial(Dictionary<string, Material> byName, Material material)
        {
            if (material == null || string.IsNullOrWhiteSpace(material.name)) return;
            if (!byName.ContainsKey(material.name)) byName.Add(material.name, material);
        }

        private static bool ContainsOrdinalIgnoreCase(List<string> values, string value)
        {
            foreach (var existing in values)
                if (string.Equals(existing, value, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static string DescribeImportedTopology(string sourcePath, GameObject sourceModel)
        {
            var parts = new List<string>();
            foreach (var renderer in sourceModel.GetComponentsInChildren<Renderer>(true))
            {
                if (parts.Count >= 8) break;
                var mesh = RivalProductionPolicy.MeshFor(renderer);
                parts.Add($"R[{renderer.name}|{(mesh == null ? "<null>" : mesh.name)}|t={TriangleCount(mesh)}]");
            }
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(sourcePath))
            {
                if (parts.Count >= 16) break;
                if (asset is Mesh mesh)
                    parts.Add($"M[{mesh.name}|t={TriangleCount(mesh)}|sub={mesh.subMeshCount}]");
            }
            return parts.Count == 0 ? "<none>" : string.Join(";", parts);
        }
    }
}
