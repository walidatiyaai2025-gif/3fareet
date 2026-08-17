using System;
using System.IO;
using Afareet.Vehicle;
using UnityEngine;

namespace Afareet.Editor
{
    /// <summary>
    /// Unity 6000.5 may flatten OBJ `o` names out of both the imported Transform hierarchy
    /// and Mesh sub-asset names. This resolver preserves the authored source contract by
    /// deriving an exact per-LOD triangle signature from the tracked OBJ text, then matching
    /// an imported source-backed Mesh only when its triangle count equals exactly one authored
    /// LOD object. It never synthesizes geometry and never falls back to broad quality bands.
    /// </summary>
    internal static class RivalImportedLodResolver
    {
        internal sealed class SourceSignature
        {
            internal readonly int[] Triangles;
            internal readonly string[] ObjectNames;

            internal SourceSignature(int[] triangles, string[] objectNames)
            {
                Triangles = triangles;
                ObjectNames = objectNames;
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

                if (currentLod < 0 || !line.StartsWith("f ", StringComparison.Ordinal))
                    continue;

                var fields = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                var vertices = fields.Length - 1;
                if (vertices < 3)
                    throw new InvalidOperationException(
                        $"UART-004 source contains an invalid face in {objectNames[currentLod]}: source={sourcePath}");
                triangles[currentLod] += vertices - 2;
            }

            for (var lod = 0; lod < lodCount; lod++)
            {
                if (!seenObjects[lod] || triangles[lod] <= 0)
                    throw new InvalidOperationException(
                        $"UART-004 source triangle signature missing LOD{lod}: source={sourcePath} expectedObjectSuffix=_LOD{lod}");

                for (var other = 0; other < lod; other++)
                {
                    if (triangles[other] == triangles[lod])
                        throw new InvalidOperationException(
                            $"UART-004 source triangle signatures are ambiguous: source={sourcePath} " +
                            $"LOD{other}=LOD{lod}={triangles[lod]} triangles");
                }
            }

            return new SourceSignature(triangles, objectNames);
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

            var mesh = RivalProductionPolicy.MeshFor(renderer);
            var meshNameLod = ResolveLodFromName(mesh == null ? string.Empty : mesh.name);
            if (meshNameLod >= 0)
                return meshNameLod;

            if (mesh == null)
                return -1;

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
    }
}
