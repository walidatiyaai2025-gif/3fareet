using UnityEngine;

namespace Afareet.World
{
    public static class RuntimeMaterials
    {
        private static Shader litShader;
        private static Shader trailShader;

        public static Material Lit(Color color, float metallic = .2f, float smoothness = .55f, float emission = 0f)
        {
            litShader ??= Resources.Load<Shader>("AfareetLit");
            if (litShader == null) throw new MissingReferenceException("AfareetLit shader is missing from Resources.");
            var material = new Material(litShader);
            material.SetColor("_Color", color);
            material.SetColor("_EmissionColor", color * emission);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", smoothness);
            return material;
        }

        public static Material Trail(Color color)
        {
            trailShader ??= Resources.Load<Shader>("AfareetTrail");
            if (trailShader == null) throw new MissingReferenceException("AfareetTrail shader is missing from Resources.");
            var material = new Material(trailShader);
            material.SetColor("_Color", color);
            return material;
        }
    }
}
