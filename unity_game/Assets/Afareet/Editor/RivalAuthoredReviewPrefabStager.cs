using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Afareet.Vehicle;
using UnityEditor;
using UnityEngine;

namespace Afareet.Editor
{
    /// <summary>
    /// Local-only UART-004 visual-review path for Unity versions whose OBJ importer merges the
    /// three authored `o ..._LOD#` objects into one `default` Mesh. The tracked OBJ remains the
    /// authority. This stager only repackages its exact existing vertex/UV/normal/face records
    /// into one temporary OBJ per authored LOD so Unity can import them independently.
    ///
    /// Review packages and prefabs are ignored by Git and carry an explicit non-production marker.
    /// They must never satisfy UART-004 production provenance or P1 verification.
    /// </summary>
    public static class RivalAuthoredReviewPrefabStager
    {
        private const string MenuPath = "Afareet/P1/Rivals/Stage Authored Review Rivals";
        private const string PackageFolder = "Assets/Afareet/ArtSource/Vehicles/Rivals/ReviewPackaging";
        private const string ReviewPrefabFolder = "Assets/Afareet/Resources/Art/Vehicles/Rivals/Review";

        private static readonly string[] SourcePaths =
        {
            "Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_01_WedgeCoupe.obj",
            "Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_02_FastbackMuscle.obj",
            "Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_03_CompactPrototype.obj"
        };

        private static readonly string[] BaseNames =
        {
            "Rival_01_WedgeCoupe",
            "Rival_02_FastbackMuscle",
            "Rival_03_CompactPrototype"
        };

        [MenuItem(MenuPath)]
        public static void StageAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("UART-004 authored review staging must run outside Play Mode.");

            ValidateCurrentSourcesOrThrow();
            EnsureAssetFolder(PackageFolder);
            EnsureAssetFolder(ReviewPrefabFolder);

            for (var variant = 0; variant < SourcePaths.Length; variant++)
                StageVariantOrThrow(variant);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log(
                "AFAREET_UART004_AUTHORED_REVIEW_STAGE_ALL_OK variants=3 source=tracked-objs " +
                "packaging=one-import-file-per-authored-lod geometryChanged=false productionGate=false p1Gate=false");
        }

        /// <summary>
        /// Read-only source validation used by the full visual-stack orchestrator before any
        /// staging mutation. Unlike the production preflight, this intentionally validates the
        /// tracked OBJ text authority rather than Unity's merged imported Mesh topology.
        /// </summary>
        public static void ValidateCurrentSourcesOrThrow()
        {
            RivalProductionPolicy.ValidateContract();
            if (SourcePaths.Length != RivalProductionPolicy.VariantCount)
                throw new InvalidOperationException("UART-004 authored review must define exactly three tracked sources.");

            var uniqueGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var variant = 0; variant < SourcePaths.Length; variant++)
            {
                var sourcePath = SourcePaths[variant];
                if (!RivalProductionPolicy.IsSupportedAuthoredModelSource(sourcePath))
                    throw new InvalidOperationException($"UART-004 review source path rejected: {sourcePath}");
                if (AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath) == null)
                    throw new InvalidOperationException($"UART-004 review source is not imported: {sourcePath}");

                var guid = AssetDatabase.AssetPathToGUID(sourcePath);
                if (string.IsNullOrWhiteSpace(guid) || !uniqueGuids.Add(guid))
                    throw new InvalidOperationException($"UART-004 review source GUID is missing/duplicated: {sourcePath}");

                var signature = RivalImportedLodResolver.ParseSourceOrThrow(sourcePath);
                for (var lod = 0; lod < signature.Triangles.Length; lod++)
                {
                    if (!RivalProductionPolicy.MeetsProductionFloor(lod, signature.Triangles[lod], true, true, true))
                        throw new InvalidOperationException(
                            $"UART-004 authored review source triangle band failed: variant={variant + 1} LOD{lod} " +
                            $"triangles={signature.Triangles[lod]}");
                }

                Debug.Log(
                    $"AFAREET_UART004_AUTHORED_REVIEW_PREFLIGHT_OK variant={variant + 1} source={sourcePath} " +
                    $"sourceSignatures={signature.Triangles[0]}/{signature.Triangles[1]}/{signature.Triangles[2]} " +
                    "authority=tracked-obj-text productionGate=false p1Gate=false");
            }

            Debug.Log(
                "AFAREET_UART004_AUTHORED_REVIEW_PREFLIGHT_ALL_OK variants=3 distinctSources=3 " +
                "authority=tracked-obj-text productionGate=false p1Gate=false");
        }

        public static string ReviewResourcePath(int variant)
        {
            ValidateVariant(variant);
            return $"Art/Vehicles/Rivals/Review/PF_Rival_{variant + 1:00}_AuthoredReview";
        }

        public static string ReviewAssetPath(int variant)
        {
            ValidateVariant(variant);
            return $"{ReviewPrefabFolder}/PF_Rival_{variant + 1:00}_AuthoredReview.prefab";
        }

        private static void StageVariantOrThrow(int variant)
        {
            var sourcePath = SourcePaths[variant];
            var signature = RivalImportedLodResolver.ParseSourceOrThrow(sourcePath);
            var packagePaths = new string[3];
            for (var lod = 0; lod < 3; lod++)
            {
                packagePaths[lod] = $"{PackageFolder}/{BaseNames[variant]}_LOD{lod}.obj";
                WriteExactLodPackageOrThrow(sourcePath, packagePaths[lod], lod, signature.ObjectNames[lod]);
                AssetDatabase.ImportAsset(
                    packagePaths[lod],
                    ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            }

            var root = new GameObject($"PF_Rival_{variant + 1:00}_AuthoredReview");
            try
            {
                var lods = new LOD[3];
                var transitions = new[] { 0.60f, 0.28f, 0.08f };
                for (var lod = 0; lod < 3; lod++)
                {
                    var imported = AssetDatabase.LoadAssetAtPath<GameObject>(packagePaths[lod]);
                    if (imported == null)
                        throw new InvalidOperationException(
                            $"UART-004 review package failed Unity import: variant={variant + 1} LOD{lod} path={packagePaths[lod]}");

                    var instance = PrefabUtility.InstantiatePrefab(imported) as GameObject;
                    if (instance == null)
                        throw new InvalidOperationException(
                            $"UART-004 review package could not instantiate: variant={variant + 1} LOD{lod}");
                    instance.name = $"Rival_{variant + 1:00}_LOD{lod}_AuthoredReview";
                    instance.transform.SetParent(root.transform, false);
                    instance.transform.localPosition = Vector3.zero;
                    instance.transform.localRotation = Quaternion.identity;
                    instance.transform.localScale = Vector3.one;

                    var renderers = instance.GetComponentsInChildren<Renderer>(true);
                    if (renderers == null || renderers.Length == 0)
                        throw new InvalidOperationException(
                            $"UART-004 review package has no renderer: variant={variant + 1} LOD{lod}");

                    var triangles = 0;
                    foreach (var renderer in renderers)
                    {
                        var mesh = RivalProductionPolicy.MeshFor(renderer);
                        if (mesh == null)
                            throw new InvalidOperationException(
                                $"UART-004 review package renderer has no mesh: variant={variant + 1} LOD{lod} renderer={renderer.name}");
                        var meshPath = AssetDatabase.GetAssetPath(mesh).Replace('\\', '/');
                        if (!string.Equals(meshPath, packagePaths[lod], StringComparison.Ordinal))
                            throw new InvalidOperationException(
                                $"UART-004 review mesh escaped package source: variant={variant + 1} LOD{lod} mesh={meshPath}");
                        triangles += RivalImportedLodResolver.TriangleCount(mesh);
                    }

                    if (triangles != signature.Triangles[lod])
                        throw new InvalidOperationException(
                            $"UART-004 review package triangle identity mismatch: variant={variant + 1} LOD{lod} " +
                            $"source={signature.Triangles[lod]} imported={triangles}");

                    lods[lod] = new LOD(transitions[lod], renderers);
                    Debug.Log(
                        $"AFAREET_UART004_AUTHORED_REVIEW_LOD_OK variant={variant + 1} lod={lod} " +
                        $"triangles={triangles} package={packagePaths[lod]} geometryChanged=false production=false");
                }

                var group = root.AddComponent<LODGroup>();
                group.SetLODs(lods);
                group.RecalculateBounds();

                var guid = AssetDatabase.AssetPathToGUID(sourcePath);
                var dependencyHash = AssetDatabase.GetAssetDependencyHash(sourcePath).ToString();
                var marker = root.AddComponent<RivalAuthoredReviewCandidateMarker>();
                marker.Configure(
                    variant,
                    sourcePath,
                    guid,
                    dependencyHash,
                    $"{signature.Triangles[0]}/{signature.Triangles[1]}/{signature.Triangles[2]}");

                var prefabPath = ReviewAssetPath(variant);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceSynchronousImport);

                var staged = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                ValidateStagedReviewOrThrow(staged, variant, signature);

                Debug.Log(
                    $"AFAREET_UART004_AUTHORED_REVIEW_STAGE_OK variant={variant + 1} source={sourcePath} " +
                    $"prefab={prefabPath} sourceGuid={guid} sourceDependencyHash={dependencyHash} " +
                    "geometryChanged=false productionGate=false p1Gate=false");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ValidateStagedReviewOrThrow(
            GameObject prefab,
            int variant,
            RivalImportedLodResolver.SourceSignature signature)
        {
            if (prefab == null)
                throw new InvalidOperationException($"UART-004 staged review prefab missing: variant={variant + 1}");

            var marker = prefab.GetComponent<RivalAuthoredReviewCandidateMarker>();
            if (marker == null ||
                marker.Classification != RivalAuthoredReviewCandidateMarker.ExpectedClassification ||
                marker.VariantIndex != variant ||
                marker.CanSatisfyProductionGate)
                throw new InvalidOperationException($"UART-004 staged review marker invalid: variant={variant + 1}");

            if (prefab.GetComponent<RivalProductionAssetMetadata>() != null)
                throw new InvalidOperationException($"UART-004 review prefab must not carry production metadata: variant={variant + 1}");

            var group = prefab.GetComponent<LODGroup>();
            var lods = group == null ? null : group.GetLODs();
            if (lods == null || lods.Length != 3)
                throw new InvalidOperationException($"UART-004 review prefab requires exactly three LODs: variant={variant + 1}");

            for (var lod = 0; lod < lods.Length; lod++)
            {
                var triangles = 0;
                foreach (var renderer in lods[lod].renderers ?? Array.Empty<Renderer>())
                {
                    var mesh = RivalProductionPolicy.MeshFor(renderer);
                    if (mesh == null)
                        throw new InvalidOperationException($"UART-004 review prefab missing mesh: variant={variant + 1} LOD{lod}");
                    var meshPath = AssetDatabase.GetAssetPath(mesh).Replace('\\', '/');
                    if (meshPath.IndexOf("/ReviewPackaging/", StringComparison.Ordinal) < 0)
                        throw new InvalidOperationException(
                            $"UART-004 review prefab contains non-review mesh: variant={variant + 1} LOD{lod} path={meshPath}");
                    triangles += RivalImportedLodResolver.TriangleCount(mesh);
                }
                if (triangles != signature.Triangles[lod])
                    throw new InvalidOperationException(
                        $"UART-004 review prefab triangle signature mismatch: variant={variant + 1} LOD{lod}");
            }
        }

        private static void WriteExactLodPackageOrThrow(
            string sourcePath,
            string destinationPath,
            int targetLod,
            string expectedObjectName)
        {
            var sourceLines = File.ReadAllLines(sourcePath);
            var globalGeometry = new List<string>();
            var mtllibs = new List<string>();
            var objectBody = new List<string>();
            var currentLod = -1;
            var foundObject = false;

            foreach (var raw in sourceLines)
            {
                var line = raw.Trim();
                if (line.StartsWith("mtllib ", StringComparison.Ordinal))
                {
                    var original = line.Substring("mtllib ".Length).Trim();
                    if (!string.IsNullOrWhiteSpace(original))
                        mtllibs.Add($"mtllib ../{Path.GetFileName(original)}");
                    continue;
                }

                if (line.StartsWith("v ", StringComparison.Ordinal) ||
                    line.StartsWith("vt ", StringComparison.Ordinal) ||
                    line.StartsWith("vn ", StringComparison.Ordinal) ||
                    line.StartsWith("vp ", StringComparison.Ordinal))
                {
                    globalGeometry.Add(raw);
                    continue;
                }

                if (line.StartsWith("o ", StringComparison.Ordinal))
                {
                    var objectName = line.Substring(2).Trim();
                    currentLod = RivalImportedLodResolver.ResolveLodFromName(objectName);
                    if (currentLod == targetLod)
                    {
                        if (!string.Equals(objectName, expectedObjectName, StringComparison.Ordinal))
                            throw new InvalidOperationException(
                                $"UART-004 review package object identity mismatch: expected={expectedObjectName} actual={objectName}");
                        foundObject = true;
                        objectBody.Add($"o {objectName}");
                    }
                    continue;
                }

                if (currentLod != targetLod)
                    continue;

                if (line.StartsWith("f ", StringComparison.Ordinal))
                {
                    var faceParts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    for (var i = 1; i < faceParts.Length; i++)
                    {
                        var vertexIndex = faceParts[i].Split('/')[0];
                        if (vertexIndex.StartsWith("-", StringComparison.Ordinal))
                            throw new InvalidOperationException(
                                $"UART-004 review packager refuses relative negative OBJ face indices: source={sourcePath} LOD{targetLod}");
                    }
                }

                if (line.StartsWith("f ", StringComparison.Ordinal) ||
                    line.StartsWith("usemtl ", StringComparison.Ordinal) ||
                    line.StartsWith("s ", StringComparison.Ordinal) ||
                    line.StartsWith("g ", StringComparison.Ordinal))
                    objectBody.Add(raw);
            }

            if (!foundObject || objectBody.Count == 0)
                throw new InvalidOperationException(
                    $"UART-004 review packager could not isolate authored LOD{targetLod}: source={sourcePath}");

            var builder = new StringBuilder();
            builder.AppendLine("# AFAREET UART-004 LOCAL AUTHORED REVIEW PACKAGE");
            builder.AppendLine($"# source={sourcePath} lod={targetLod} geometryChanged=false productionGate=false");
            foreach (var mtllib in mtllibs) builder.AppendLine(mtllib);
            foreach (var geometryLine in globalGeometry) builder.AppendLine(geometryLine);
            foreach (var bodyLine in objectBody) builder.AppendLine(bodyLine);

            var fullPath = Path.GetFullPath(destinationPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? PackageFolder);
            File.WriteAllText(fullPath, builder.ToString(), new UTF8Encoding(false));
        }

        private static void EnsureAssetFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder)) return;
            var parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
            var leaf = Path.GetFileName(assetFolder);
            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(leaf))
                throw new InvalidOperationException($"Invalid Unity asset folder: {assetFolder}");
            EnsureAssetFolder(parent);
            if (!AssetDatabase.IsValidFolder(assetFolder))
                AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void ValidateVariant(int variant)
        {
            if (variant < 0 || variant >= RivalProductionPolicy.VariantCount)
                throw new ArgumentOutOfRangeException(nameof(variant));
        }
    }
}
