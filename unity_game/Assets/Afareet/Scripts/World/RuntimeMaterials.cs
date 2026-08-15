using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afareet.World
{
    public static class RuntimeMaterials
    {
        private static readonly Dictionary<LitKey, Material> LitCache = new();
        private static readonly Dictionary<Color32, Material> TrailCache = new();
        private static Shader litShader;
        private static Shader trailShader;

        public static int CachedMaterialCount => LitCache.Count + TrailCache.Count;

        public static Material Lit(Color color, float metallic = .2f, float smoothness = .55f, float emission = 0f)
        {
            var key = new LitKey((Color32)color, metallic, smoothness, emission);
            if (LitCache.TryGetValue(key, out var cached) && cached != null) return cached;

            litShader ??= Resources.Load<Shader>("AfareetLit");
            if (litShader == null) throw new MissingReferenceException("AfareetLit shader is missing from Resources.");

            var material = new Material(litShader)
            {
                name = $"AFAREET_LIT_{key.GetHashCode():X8}"
            };
            material.SetColor("_Color", color);
            material.SetColor("_EmissionColor", color * emission);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", smoothness);
            LitCache[key] = material;
            return material;
        }

        public static Material Trail(Color color)
        {
            var key = (Color32)color;
            if (TrailCache.TryGetValue(key, out var cached) && cached != null) return cached;

            trailShader ??= Resources.Load<Shader>("AfareetTrail");
            if (trailShader == null) throw new MissingReferenceException("AfareetTrail shader is missing from Resources.");

            var material = new Material(trailShader)
            {
                name = $"AFAREET_TRAIL_{key.GetHashCode():X8}"
            };
            material.SetColor("_Color", color);
            TrailCache[key] = material;
            return material;
        }

        private readonly struct LitKey : IEquatable<LitKey>
        {
            private readonly Color32 color;
            private readonly int metallic;
            private readonly int smoothness;
            private readonly int emission;

            public LitKey(Color32 color, float metallic, float smoothness, float emission)
            {
                this.color = color;
                this.metallic = Mathf.RoundToInt(metallic * 1000f);
                this.smoothness = Mathf.RoundToInt(smoothness * 1000f);
                this.emission = Mathf.RoundToInt(emission * 1000f);
            }

            public bool Equals(LitKey other) =>
                color.Equals(other.color) &&
                metallic == other.metallic &&
                smoothness == other.smoothness &&
                emission == other.emission;

            public override bool Equals(object obj) => obj is LitKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = color.GetHashCode();
                    hash = (hash * 397) ^ metallic;
                    hash = (hash * 397) ^ smoothness;
                    hash = (hash * 397) ^ emission;
                    return hash;
                }
            }
        }
    }
}
