using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Afareet.Vehicle;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Afareet.Editor
{
    /// <summary>
    /// Deterministically converts the tracked UART-004 authored design profiles into the
    /// three runtime rival prefabs. This replaces presentation only; physics stays on the
    /// existing CarFactory rival roots.
    /// </summary>
    public static class RivalProductionAssetBuilder
    {
        private const string OutputRoot = "Assets/Afareet/Resources/Art/Vehicles/Rivals/Production";
        private const string SourceRelativePath = "docs/assets/01_vehicles/rival_cars_production/RIVAL_DESIGN_PROFILES.json";
        private static readonly string[] Groups = { "Body", "Glass", "Wheel", "Aero", "Light", "Accent" };

        [Serializable]
        private sealed class DesignPack
        {
            public string version;
            public string reviewState;
            public RivalProfile[] variants;
            public LodTopology[] lodTopology;
        }

        [Serializable]
        private sealed class RivalProfile
        {
            public string id;
            public string displayName;
            public float length;
            public float width;
            public float bodyHeight;
            public float roofHeight;
            public float roofWidthFactor;
            public float rearWingWidth;
            public float haunchFlare;
            public float wheelRadius;
            public string primary;
            public string secondary;
        }

        [Serializable]
        private sealed class LodTopology
        {
            public int lod;
            public int longitudinalSegments;
            public int bodyRadialSegments;
            public int wheelSegments;
        }

        private sealed class MeshDraft
        {
            public readonly List<Vector3> vertices = new();
            public readonly List<Vector2> uv = new();
            public readonly List<Vector3> normals = new();
            public readonly List<string> groupOrder = new();
            public readonly Dictionary<string, List<int>> triangles = new(StringComparer.Ordinal);

            public int AddVertex(Vector3 position, Vector2 texcoord, Vector3 normal)
            {
                vertices.Add(position);
                uv.Add(texcoord);
                normals.Add(normal.sqrMagnitude < 0.0001f ? Vector3.up : normal.normalized);
                return vertices.Count - 1;
            }

            public void AddTri(string group, int a, int b, int c)
            {
                if (!triangles.TryGetValue(group, out var list))
                {
                    list = new List<int>();
                    triangles[group] = list;
                    groupOrder.Add(group);
                }
                list.Add(a); list.Add(b); list.Add(c);
            }

            public int TriangleCount
            {
                get
                {
                    var count = 0;
                    foreach (var pair in triangles) count += pair.Value.Count / 3;
                    return count;
                }
            }
        }

        [MenuItem("Afareet/Build UART-004 Production Rivals")]
        public static void BuildMenu() => BuildOrThrow();

        [MenuItem("Afareet/Validate UART-004 Production Rivals")]
        public static void ValidateMenu()
        {
            ValidateOrThrow();
            Debug.Log("AFAREET_UART004_RIVAL_ASSET_VALIDATION_OK");
        }

        [InitializeOnLoadMethod]
        private static void ScheduleEditorGeneration()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += () =>
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(RivalProductionPolicy.AssetPath(0)) != null) return;
                try { BuildOrThrow(); }
                catch (Exception ex) { Debug.LogError($"AFAREET_UART004_RIVAL_GENERATION_FAILED {ex}"); }
            };
        }

        public static void BuildOrThrow()
        {
            RivalProductionPolicy.ValidateContract();
            var sourcePath = SourcePath();
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException($"UART-004 authored design source missing: {sourcePath}", sourcePath);

            var sourceBytes = File.ReadAllBytes(sourcePath);
            var pack = JsonUtility.FromJson<DesignPack>(Encoding.UTF8.GetString(sourceBytes));
            ValidatePack(pack);
            var sourceSha = Sha256(sourceBytes);

            if (AssetDatabase.IsValidFolder(OutputRoot)) AssetDatabase.DeleteAsset(OutputRoot);
            Directory.CreateDirectory(OutputRoot);
            AssetDatabase.Refresh();

            for (var variant = 0; variant < RivalProductionPolicy.VariantCount; variant++)
                BuildVariant(pack, variant, sourceSha);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateOrThrow();
            Debug.Log($"AFAREET_UART004_RIVAL_ASSET_BUILD_OK variants=3 sourceSha256={sourceSha}");
        }

        public static void ValidateOrThrow()
        {
            RivalProductionPolicy.ValidateContract();
            for (var variant = 0; variant < RivalProductionPolicy.VariantCount; variant++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RivalProductionPolicy.AssetPath(variant));
                if (!RivalProductionPolicy.ValidateProductionPrefab(prefab, variant, out var reason))
                    throw new InvalidOperationException($"UART-004 rival {variant + 1} validation failed: {reason}");
            }
        }

        private static void BuildVariant(DesignPack pack, int variant, string sourceSha)
        {
            var profile = pack.variants[variant];
            var materials = CreateMaterials(profile, variant);
            var renderers = new Renderer[3];
            var root = new GameObject($"PF_Rival_{variant + 1:00}_Production");
            var metadata = root.AddComponent<RivalProductionAssetMetadata>();
            metadata.Configure(
                variant,
                true,
                true,
                true,
                true,
                profile.id,
                pack.version,
                $"{sourceSha}:{profile.id}");

            for (var lod = 0; lod < 3; lod++)
            {
                var topology = pack.lodTopology[lod];
                var draft = BuildDraft(profile, variant, topology);
                var mesh = CreateMesh(draft, variant, lod);
                if (!RivalProductionPolicy.MeetsProductionFloor(lod, draft.TriangleCount, true, true, true))
                    throw new InvalidOperationException(
                        $"UART-004 generated rival {variant + 1} LOD{lod} outside policy: triangles={draft.TriangleCount}");

                var meshPath = $"{OutputRoot}/SM_Rival_{variant + 1:00}_LOD{lod}.asset";
                AssetDatabase.CreateAsset(mesh, meshPath);

                var child = new GameObject($"SM_Rival_{variant + 1:00}_LOD{lod}");
                child.transform.SetParent(root.transform, false);
                var filter = child.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                var renderer = child.AddComponent<MeshRenderer>();
                var bindings = new Material[draft.groupOrder.Count];
                for (var sub = 0; sub < bindings.Length; sub++) bindings[sub] = materials[draft.groupOrder[sub]];
                renderer.sharedMaterials = bindings;
                renderer.shadowCastingMode = lod == 2 ? ShadowCastingMode.Off : ShadowCastingMode.On;
                renderer.receiveShadows = lod < 2;
                renderers[lod] = renderer;
            }

            var group = root.AddComponent<LODGroup>();
            group.fadeMode = LODFadeMode.None;
            group.animateCrossFading = false;
            group.SetLODs(new[]
            {
                new LOD(.58f, new[] { renderers[0] }),
                new LOD(.27f, new[] { renderers[1] }),
                new LOD(.08f, new[] { renderers[2] })
            });
            group.RecalculateBounds();

            PrefabUtility.SaveAsPrefabAsset(root, RivalProductionPolicy.AssetPath(variant));
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static MeshDraft BuildDraft(RivalProfile p, int variant, LodTopology topology)
        {
            var d = new MeshDraft();
            BuildBody(d, p, variant, topology.longitudinalSegments, topology.bodyRadialSegments);
            BuildCabin(d, p, variant, topology.longitudinalSegments, topology.bodyRadialSegments);
            BuildWheels(d, p, topology.wheelSegments);
            BuildAeroAndIdentity(d, p, variant);
            return d;
        }

        private static void BuildBody(MeshDraft d, RivalProfile p, int variant, int longitudinal, int radial)
        {
            var rings = new int[longitudinal + 1, radial];
            var halfLength = p.length * .5f;
            for (var iz = 0; iz <= longitudinal; iz++)
            {
                var t = iz / (float)longitudinal;
                var z = -halfLength + t * p.length;
                var longitudinalShape = Mathf.Pow(Mathf.Max(.05f, Mathf.Sin(Mathf.PI * t)), .38f);
                var haunch = 1f + p.haunchFlare * Mathf.Exp(-Mathf.Pow((t - .31f) / .18f, 2f));
                var halfWidth = p.width * .5f * (.42f + .58f * longitudinalShape) * haunch;
                var top = p.bodyHeight * (.48f + .52f * longitudinalShape);
                if (variant == 0) top -= .10f * t;
                else if (variant == 1) top += .04f * t + .10f * (1f - t);
                else top -= .16f * t + .02f * (1f - t);
                var bottom = .20f + .03f * Mathf.Cos(Mathf.PI * t);

                for (var ia = 0; ia < radial; ia++)
                {
                    var angle = Mathf.PI * 2f * ia / radial;
                    var x = halfWidth * Mathf.Cos(angle);
                    var yWave = Mathf.Sin(angle);
                    var y = (yWave >= 0f ? top : bottom) * yWave + .48f;
                    y = Mathf.Max(.18f, y);
                    rings[iz, ia] = d.AddVertex(
                        new Vector3(x, y, z),
                        new Vector2(ia / (float)radial, t),
                        new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * 1.3f, 0f));
                }
            }

            for (var iz = 0; iz < longitudinal; iz++)
            for (var ia = 0; ia < radial; ia++)
            {
                var next = (ia + 1) % radial;
                var a0 = rings[iz, ia]; var a1 = rings[iz, next];
                var b0 = rings[iz + 1, ia]; var b1 = rings[iz + 1, next];
                d.AddTri("Body", a0, b0, b1);
                d.AddTri("Body", a0, b1, a1);
            }
        }

        private static void BuildCabin(MeshDraft d, RivalProfile p, int variant, int longitudinal, int radial)
        {
            var around = Mathf.Max(6, radial / 2);
            var along = Mathf.Max(6, longitudinal / 3);
            var rings = new int[along + 1, around + 1];
            var z0 = variant == 0 ? -.90f : variant == 1 ? -1.10f : -.72f;
            var z1 = variant == 0 ? .92f : variant == 1 ? 1.18f : .70f;

            for (var iz = 0; iz <= along; iz++)
            {
                var t = iz / (float)along;
                var z = Mathf.Lerp(z0, z1, t);
                var arch = Mathf.Pow(Mathf.Max(.05f, Mathf.Sin(Mathf.PI * t)), .55f);
                var halfWidth = p.width * .5f * p.roofWidthFactor * (.70f + .30f * arch);
                var roofHeight = p.roofHeight * arch;
                for (var ia = 0; ia <= around; ia++)
                {
                    var angle = Mathf.PI * ia / around;
                    var x = halfWidth * Mathf.Cos(angle);
                    var y = .69f + roofHeight * Mathf.Sin(angle);
                    if (variant == 1) y += .10f * (1f - t);
                    rings[iz, ia] = d.AddVertex(
                        new Vector3(x, y, z),
                        new Vector2(ia / (float)around, t),
                        new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f));
                }
            }

            for (var iz = 0; iz < along; iz++)
            for (var ia = 0; ia < around; ia++)
            {
                var a0 = rings[iz, ia]; var a1 = rings[iz, ia + 1];
                var b0 = rings[iz + 1, ia]; var b1 = rings[iz + 1, ia + 1];
                d.AddTri("Glass", a0, b1, b0);
                d.AddTri("Glass", a0, a1, b1);
            }
        }

        private static void BuildWheels(MeshDraft d, RivalProfile p, int segments)
        {
            var halfLength = p.length * .5f;
            var wheelZ = new[] { -halfLength * .58f, halfLength * .58f };
            var wheelX = p.width * .5f * .98f;
            const float thickness = .22f;

            foreach (var z in wheelZ)
            foreach (var side in new[] { -1f, 1f })
            {
                var centerX = side * wheelX;
                var ringA = new int[segments];
                var ringB = new int[segments];
                for (var k = 0; k < segments; k++)
                {
                    var angle = Mathf.PI * 2f * k / segments;
                    var y = .43f + p.wheelRadius * Mathf.Cos(angle);
                    var zz = z + p.wheelRadius * Mathf.Sin(angle);
                    var normal = new Vector3(side * .15f, Mathf.Cos(angle), Mathf.Sin(angle));
                    ringA[k] = d.AddVertex(new Vector3(centerX - thickness * .5f, y, zz), new Vector2(k / (float)segments, 0f), normal);
                    ringB[k] = d.AddVertex(new Vector3(centerX + thickness * .5f, y, zz), new Vector2(k / (float)segments, 1f), normal);
                }

                for (var k = 0; k < segments; k++)
                {
                    var next = (k + 1) % segments;
                    d.AddTri("Wheel", ringA[k], ringB[next], ringB[k]);
                    d.AddTri("Wheel", ringA[k], ringA[next], ringB[next]);
                }

                var centerA = d.AddVertex(new Vector3(centerX - thickness * .5f, .43f, z), new Vector2(.5f, .5f), Vector3.left * side);
                var centerB = d.AddVertex(new Vector3(centerX + thickness * .5f, .43f, z), new Vector2(.5f, .5f), Vector3.right * side);
                for (var k = 0; k < segments; k++)
                {
                    var next = (k + 1) % segments;
                    d.AddTri("Wheel", centerA, ringA[next], ringA[k]);
                    d.AddTri("Wheel", centerB, ringB[k], ringB[next]);
                }
            }
        }

        private static void BuildAeroAndIdentity(MeshDraft d, RivalProfile p, int variant)
        {
            var halfLength = p.length * .5f;
            var rearZ = -halfLength * .92f;
            var wingY = variant == 1 ? 1.22f : 1.10f;
            AddBox(d, "Aero", new Vector3(0f, wingY, rearZ), new Vector3(p.rearWingWidth, .08f, .32f));
            AddBox(d, "Aero", new Vector3(-.55f, wingY - .20f, rearZ), new Vector3(.08f, .40f, .10f));
            AddBox(d, "Aero", new Vector3(.55f, wingY - .20f, rearZ), new Vector3(.08f, .40f, .10f));
            AddBox(d, "Aero", new Vector3(0f, .22f, halfLength * .97f), new Vector3(p.width * .92f, .06f, .30f));

            if (variant == 1)
            {
                AddBox(d, "Aero", new Vector3(-p.width * .47f, .34f, -.15f), new Vector3(.10f, .18f, 1.50f));
                AddBox(d, "Aero", new Vector3(p.width * .47f, .34f, -.15f), new Vector3(.10f, .18f, 1.50f));
            }
            else if (variant == 2)
            {
                AddBox(d, "Aero", new Vector3(0f, 1.10f, -.25f), new Vector3(.06f, .48f, 1.20f));
            }

            AddBox(d, "Light", new Vector3(0f, .62f, halfLength * .985f), new Vector3(p.width * .62f, .10f, .04f));
            AddBox(d, "Accent", new Vector3(0f, .52f, -halfLength * .985f), new Vector3(p.width * .50f, .07f, .04f));

            if (variant == 0)
                AddBox(d, "Accent", new Vector3(0f, .88f, .65f), new Vector3(.12f, .04f, 1.0f));
            else if (variant == 1)
                AddBox(d, "Accent", new Vector3(0f, .82f, -.35f), new Vector3(.22f, .05f, 1.70f));
            else
            {
                AddBox(d, "Accent", new Vector3(-.58f, .77f, .40f), new Vector3(.08f, .05f, 1.15f));
                AddBox(d, "Accent", new Vector3(.58f, .77f, .40f), new Vector3(.08f, .05f, 1.15f));
            }
        }

        private static void AddBox(MeshDraft d, string group, Vector3 center, Vector3 size)
        {
            var h = size * .5f;
            var p = new[]
            {
                center + new Vector3(-h.x,-h.y,-h.z), center + new Vector3(h.x,-h.y,-h.z),
                center + new Vector3(h.x,h.y,-h.z), center + new Vector3(-h.x,h.y,-h.z),
                center + new Vector3(-h.x,-h.y,h.z), center + new Vector3(h.x,-h.y,h.z),
                center + new Vector3(h.x,h.y,h.z), center + new Vector3(-h.x,h.y,h.z)
            };
            var ids = new int[8];
            for (var i = 0; i < 8; i++) ids[i] = d.AddVertex(p[i], new Vector2((i & 1) == 0 ? 0f : 1f, (i & 2) == 0 ? 0f : 1f), (p[i] - center).normalized);
            var faces = new[]
            {
                0,2,1, 0,3,2, 4,5,6, 4,6,7, 0,1,5, 0,5,4,
                3,7,6, 3,6,2, 0,4,7, 0,7,3, 1,2,6, 1,6,5
            };
            for (var i = 0; i < faces.Length; i += 3) d.AddTri(group, ids[faces[i]], ids[faces[i + 1]], ids[faces[i + 2]]);
        }

        private static Mesh CreateMesh(MeshDraft d, int variant, int lod)
        {
            var mesh = new Mesh
            {
                name = $"SM_Rival_{variant + 1:00}_LOD{lod}",
                indexFormat = IndexFormat.UInt16
            };
            mesh.SetVertices(d.vertices);
            mesh.SetUVs(0, d.uv);
            mesh.SetNormals(d.normals);
            mesh.subMeshCount = d.groupOrder.Count;
            for (var sub = 0; sub < d.groupOrder.Count; sub++) mesh.SetTriangles(d.triangles[d.groupOrder[sub]], sub, true);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        private static Dictionary<string, Material> CreateMaterials(RivalProfile p, int variant)
        {
            var shader = Shader.Find("Afareet/RuntimeLit");
            if (shader == null) throw new InvalidOperationException("Required shader Afareet/RuntimeLit is unavailable.");
            if (!ColorUtility.TryParseHtmlString(p.primary, out var primary)) primary = Color.magenta;
            if (!ColorUtility.TryParseHtmlString(p.secondary, out var secondary)) secondary = Color.cyan;

            var colors = new Dictionary<string, Color>(StringComparer.Ordinal)
            {
                ["Body"] = primary,
                ["Glass"] = new Color(.025f, .11f, .16f, 1f),
                ["Wheel"] = new Color(.012f, .014f, .018f, 1f),
                ["Aero"] = Color.Lerp(primary, Color.black, .58f),
                ["Light"] = new Color(.85f, .93f, 1f, 1f),
                ["Accent"] = secondary
            };

            var result = new Dictionary<string, Material>(StringComparer.Ordinal);
            foreach (var group in Groups)
            {
                var texture = CreateTextureMap(variant, group, colors[group], secondary);
                var material = new Material(shader) { name = $"M_Rival_{variant + 1:00}_{group}" };
                material.mainTexture = texture;
                if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", group == "Body" || group == "Aero" ? .62f : .18f);
                if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", group == "Glass" ? .92f : .68f);
                if (material.HasProperty("_EmissionColor"))
                    material.SetColor("_EmissionColor", group == "Light" || group == "Accent" ? colors[group] * 1.8f : Color.black);
                AssetDatabase.CreateAsset(material, $"{OutputRoot}/{material.name}.mat");
                result[group] = material;
            }
            return result;
        }

        private static Texture2D CreateTextureMap(int variant, string group, Color primary, Color secondary)
        {
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false)
            {
                name = $"TX_Rival_{variant + 1:00}_{group}_ProfileMap",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
            for (var y = 0; y < 16; y++)
            for (var x = 0; x < 16; x++)
            {
                var stripe = ((x + y + variant * 3) % 7) == 0;
                texture.SetPixel(x, y, stripe ? Color.Lerp(primary, secondary, .28f) : primary);
            }
            texture.Apply(false, false);
            AssetDatabase.CreateAsset(texture, $"{OutputRoot}/{texture.name}.asset");
            return texture;
        }

        private static void ValidatePack(DesignPack pack)
        {
            if (pack == null || string.IsNullOrWhiteSpace(pack.version)) throw new InvalidDataException("UART-004 design pack version missing.");
            if (pack.variants == null || pack.variants.Length != RivalProductionPolicy.VariantCount)
                throw new InvalidDataException("UART-004 design pack must define exactly three rival variants.");
            if (pack.lodTopology == null || pack.lodTopology.Length != 3)
                throw new InvalidDataException("UART-004 design pack must define exactly three LOD topology profiles.");
            for (var i = 0; i < pack.variants.Length; i++)
            {
                var p = pack.variants[i];
                if (p == null || string.IsNullOrWhiteSpace(p.id) || p.length <= 4f || p.width <= 1.7f || p.bodyHeight <= .5f)
                    throw new InvalidDataException($"UART-004 rival profile {i + 1} is incomplete.");
            }
            for (var lod = 0; lod < pack.lodTopology.Length; lod++)
            {
                var t = pack.lodTopology[lod];
                if (t == null || t.lod != lod || t.longitudinalSegments < 8 || t.bodyRadialSegments < 6 || t.wheelSegments < 6)
                    throw new InvalidDataException($"UART-004 LOD topology {lod} is invalid.");
            }
        }

        private static string SourcePath()
        {
            var repositoryRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            return Path.Combine(repositoryRoot, SourceRelativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string Sha256(byte[] bytes)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
