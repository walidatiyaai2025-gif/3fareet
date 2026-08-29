using System;
using System.Collections.Generic;
using System.IO;

namespace Afareet.Editor
{
    /// <summary>
    /// Source-level production material provenance for tracked OBJ handoffs.
    /// A production OBJ must use tracked MTL definitions and every used material must
    /// resolve to at least one tracked texture inside the same source root.
    /// </summary>
    internal static class ObjProductionMaterialDependencyPolicy
    {
        private static readonly string[] TextureDirectives =
        {
            "map_Kd", "map_Ka", "map_Ks", "map_d", "map_Bump", "bump", "norm", "disp", "decal"
        };

        public static void ValidateOrThrow(string objPath, string sourceRoot, Action<string> fail)
        {
            if (fail == null) throw new ArgumentNullException(nameof(fail));
            if (string.IsNullOrWhiteSpace(objPath) || !File.Exists(objPath))
            {
                fail($"OBJ source is missing: {objPath}");
                return;
            }
            if (string.IsNullOrWhiteSpace(sourceRoot) || !Directory.Exists(sourceRoot))
            {
                fail($"OBJ source root is missing: {sourceRoot}");
                return;
            }

            var root = Path.GetFullPath(sourceRoot);
            var obj = Path.GetFullPath(objPath);
            if (!IsInsideRoot(obj, root))
            {
                fail($"OBJ source escapes tracked source root: {obj}");
                return;
            }

            var materialLibraries = new List<string>();
            var usedMaterials = new HashSet<string>(StringComparer.Ordinal);
            foreach (var raw in File.ReadLines(obj))
            {
                var line = raw.Trim();
                if (line.StartsWith("mtllib ", StringComparison.Ordinal))
                {
                    var reference = CleanReference(line.Substring(7));
                    if (string.IsNullOrWhiteSpace(reference))
                    {
                        fail($"OBJ has an empty mtllib declaration: {obj}");
                        return;
                    }
                    materialLibraries.Add(reference);
                }
                else if (line.StartsWith("usemtl ", StringComparison.Ordinal))
                {
                    var material = line.Substring(7).Trim();
                    if (!string.IsNullOrWhiteSpace(material)) usedMaterials.Add(material);
                }
            }

            if (materialLibraries.Count == 0)
            {
                fail($"Production OBJ has no mtllib declaration: {obj}");
                return;
            }
            if (usedMaterials.Count == 0)
            {
                fail($"Production OBJ has no usemtl assignments: {obj}");
                return;
            }

            var definedMaterials = new HashSet<string>(StringComparer.Ordinal);
            var texturedMaterials = new HashSet<string>(StringComparer.Ordinal);
            var trackedTextureCount = 0;

            foreach (var libraryReference in materialLibraries)
            {
                var mtl = ResolveTrackedDependency(root, libraryReference, ".mtl", fail);
                if (mtl == null) return;

                string currentMaterial = null;
                foreach (var raw in File.ReadLines(mtl))
                {
                    var line = raw.Trim();
                    if (line.StartsWith("newmtl ", StringComparison.Ordinal))
                    {
                        currentMaterial = line.Substring(7).Trim();
                        if (!string.IsNullOrWhiteSpace(currentMaterial))
                            definedMaterials.Add(currentMaterial);
                        continue;
                    }

                    if (!TryTextureReference(line, out var textureReference)) continue;
                    if (string.IsNullOrWhiteSpace(currentMaterial))
                    {
                        fail($"MTL texture mapping appears before newmtl: {mtl}");
                        return;
                    }

                    var texture = ResolveTrackedTexture(root, textureReference, fail);
                    if (texture == null) return;
                    trackedTextureCount++;
                    texturedMaterials.Add(currentMaterial);
                }
            }

            if (trackedTextureCount == 0)
            {
                fail($"Production MTL set has no tracked texture dependency for OBJ: {obj}");
                return;
            }

            foreach (var material in usedMaterials)
            {
                if (!definedMaterials.Contains(material))
                {
                    fail($"OBJ usemtl is not defined by tracked MTL files: material={material} obj={obj}");
                    return;
                }
                if (!texturedMaterials.Contains(material))
                {
                    fail($"OBJ production material has no tracked texture map: material={material} obj={obj}");
                    return;
                }
            }
        }

        private static string ResolveTrackedDependency(string root, string reference, string requiredExtension, Action<string> fail)
        {
            if (Path.IsPathRooted(reference))
            {
                fail($"Absolute material dependency is forbidden: {reference}");
                return null;
            }

            var resolved = Path.GetFullPath(Path.Combine(root, reference.Replace('/', Path.DirectorySeparatorChar)));
            if (!IsInsideRoot(resolved, root))
            {
                fail($"Material dependency escapes tracked source root: {reference}");
                return null;
            }
            if (!string.Equals(Path.GetExtension(resolved), requiredExtension, StringComparison.OrdinalIgnoreCase))
            {
                fail($"Unexpected material dependency type: {reference}");
                return null;
            }
            if (!File.Exists(resolved))
            {
                fail($"Tracked material dependency is missing: {reference}");
                return null;
            }
            return resolved;
        }

        private static string ResolveTrackedTexture(string root, string reference, Action<string> fail)
        {
            if (Path.IsPathRooted(reference))
            {
                fail($"Absolute texture dependency is forbidden: {reference}");
                return null;
            }

            var resolved = Path.GetFullPath(Path.Combine(root, reference.Replace('/', Path.DirectorySeparatorChar)));
            if (!IsInsideRoot(resolved, root))
            {
                fail($"Texture dependency escapes tracked source root: {reference}");
                return null;
            }
            if (!IsSupportedTexture(Path.GetExtension(resolved)))
            {
                fail($"Unsupported production texture dependency: {reference}");
                return null;
            }
            if (!File.Exists(resolved))
            {
                fail($"Tracked texture dependency is missing: {reference}");
                return null;
            }
            return resolved;
        }

        private static bool TryTextureReference(string line, out string reference)
        {
            reference = string.Empty;
            foreach (var directive in TextureDirectives)
            {
                if (!line.StartsWith(directive + " ", StringComparison.OrdinalIgnoreCase)) continue;
                var remainder = line.Substring(directive.Length).Trim();
                reference = LastReferenceToken(remainder);
                return !string.IsNullOrWhiteSpace(reference);
            }
            return false;
        }

        private static string LastReferenceToken(string value)
        {
            value = (value ?? string.Empty).Trim();
            if (value.Length == 0) return string.Empty;

            if (value.EndsWith("\"", StringComparison.Ordinal))
            {
                var opening = value.LastIndexOf('"', value.Length - 2);
                if (opening >= 0) return value.Substring(opening + 1, value.Length - opening - 2).Trim();
            }

            var tokens = value.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            return tokens.Length == 0 ? string.Empty : CleanReference(tokens[tokens.Length - 1]);
        }

        private static string CleanReference(string value)
        {
            return (value ?? string.Empty).Trim().Trim('"').Replace('\\', '/');
        }

        private static bool IsInsideRoot(string path, string root)
        {
            var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var normalizedPath = Path.GetFullPath(path);
            return normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSupportedTexture(string extension)
        {
            switch ((extension ?? string.Empty).ToLowerInvariant())
            {
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".tga":
                case ".bmp":
                case ".exr":
                case ".psd":
                    return true;
                default:
                    return false;
            }
        }
    }
}
