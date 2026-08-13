using System.Collections.Generic;
using UnityEngine;

namespace Afareet.World
{
    public sealed class TrackRuntime
    {
        public readonly List<Transform> Waypoints = new();
        public Vector3 StartPosition => Waypoints[0].position + Vector3.up * 0.65f;
        public Quaternion StartRotation => Waypoints[0].rotation;
        public Vector3 GridPosition(int index) => StartPosition - Waypoints[0].forward * (index * 5f) + Waypoints[0].right * ((index % 2 == 0 ? -1 : 1) * 2.2f);
    }

    public static class CairoTrackBuilder
    {
        private const int SegmentCount = 72;
        private const float RadiusX = 92f;
        private const float RadiusZ = 58f;
        private const float RoadWidth = 14f;

        public static TrackRuntime Build(Transform parent)
        {
            var track = new TrackRuntime();
            var root = new GameObject("CAIRO NIGHT RUN // 3FAREET").transform;
            root.SetParent(parent);
            CreateGround(root);

            var asphalt = Material(new Color(.022f, .028f, .052f), .18f, .58f);
            var cyan = Emissive(new Color(0f, .72f, 1f), 4.2f);
            var purple = Emissive(new Color(.5f, .03f, .95f), 4.8f);
            var gold = Emissive(new Color(1f, .48f, .06f), 3.5f);
            var magenta = Emissive(new Color(1f, .06f, .48f), 3.8f);

            for (var i = 0; i < SegmentCount; i++)
            {
                var t = i / (float)SegmentCount * Mathf.PI * 2f;
                var nextT = (i + 1) / (float)SegmentCount * Mathf.PI * 2f;
                var p = Point(t);
                var next = Point(nextT);
                var direction = (next - p).normalized;
                var length = Vector3.Distance(p, next) + .3f;
                var rotation = Quaternion.LookRotation(direction);

                var road = Cube(root, $"Road {i:00}", (p + next) * .5f, new Vector3(RoadWidth, .28f, length), asphalt, rotation);
                road.layer = 0;

                var waypoint = new GameObject($"Waypoint {i:00}").transform;
                waypoint.SetParent(root);
                waypoint.SetPositionAndRotation(p + Vector3.up * .3f, rotation);
                track.Waypoints.Add(waypoint);

                var right = rotation * Vector3.right;
                var leftGlow = i % 3 == 0 ? purple : (i % 2 == 0 ? cyan : gold);
                var rightGlow = i % 5 == 0 ? magenta : (i % 2 == 0 ? gold : cyan);
                CreateNeonRail(root, p + right * (RoadWidth * .53f), rotation, length, leftGlow);
                CreateNeonRail(root, p - right * (RoadWidth * .53f), rotation, length, rightGlow);

                if (i % 6 == 0) CreateRoadRune(root, (p + next) * .5f + Vector3.up * .18f, rotation, i, purple, gold);
                if (i % 9 == 0) CreateLightTotem(root, p + right * (RoadWidth * .72f), rotation, i % 18 == 0 ? purple : cyan);
                if (i % 4 == 0) CreateBuilding(root, p + right * (RoadWidth + 8f), i, gold, purple);
                if (i % 5 == 0) CreateBuilding(root, p - right * (RoadWidth + 10f), i + 17, cyan, magenta);
            }

            CreatePyramids(root, purple, gold);
            CreateFinishGate(root, track.Waypoints[0], cyan, purple, gold);
            return track;
        }

        private static Vector3 Point(float t) => new(RadiusX * Mathf.Cos(t), 0f, RadiusZ * Mathf.Sin(t));

        private static void CreateGround(Transform root)
        {
            var ground = Cube(root, "Desert Ground", Vector3.down * .35f, new Vector3(260f, .5f, 210f), Material(new Color(.052f, .035f, .065f), 0f, .14f), Quaternion.identity);
            ground.GetComponent<Collider>().isTrigger = false;
        }

        private static void CreateNeonRail(Transform root, Vector3 position, Quaternion rotation, float length, Material glow)
        {
            Cube(root, "Neon Rail", position + Vector3.up * .35f, new Vector3(.18f, .18f, length), glow, rotation);
        }

        private static void CreateRoadRune(Transform root, Vector3 position, Quaternion rotation, int seed, Material primary, Material secondary)
        {
            var mat = seed % 12 == 0 ? secondary : primary;
            Cube(root, "Asphalt Spirit Rune", position, new Vector3(.22f, .035f, 4.2f), mat, rotation);
            Cube(root, "Asphalt Spirit Rune Wing L", position - rotation * Vector3.right * .72f, new Vector3(.12f, .03f, 2.2f), mat, rotation * Quaternion.Euler(0f, -18f, 0f));
            Cube(root, "Asphalt Spirit Rune Wing R", position + rotation * Vector3.right * .72f, new Vector3(.12f, .03f, 2.2f), mat, rotation * Quaternion.Euler(0f, 18f, 0f));
        }

        private static void CreateLightTotem(Transform root, Vector3 position, Quaternion rotation, Material glow)
        {
            Cube(root, "Spirit Light Totem", position + Vector3.up * 2.2f, new Vector3(.18f, 4.4f, .18f), Material(new Color(.04f, .025f, .06f), .15f, .45f), rotation);
            Cube(root, "Spirit Light Blade", position + Vector3.up * 3.8f, new Vector3(.65f, 1.6f, .12f), glow, rotation);
        }

        private static void CreateBuilding(Transform root, Vector3 position, int seed, Material windowMaterial, Material accentMaterial)
        {
            var height = 8f + (seed * 7 % 17);
            var width = 5f + (seed * 3 % 6);
            var rotation = Quaternion.Euler(0f, seed * 31f, 0f);
            var building = Cube(root, "Cairo Building", position + Vector3.up * height * .5f, new Vector3(width, height, width), Material(new Color(.055f, .038f, .075f), .18f, .38f), rotation);
            for (var floor = 2; floor < height - 1; floor += 3)
                Cube(building.transform, "Warm Window", new Vector3(0f, floor - height * .5f, -width * .505f), new Vector3(width * .55f, .5f, .05f), windowMaterial, Quaternion.identity, true);

            Cube(root, "Roof Neon Crown", position + Vector3.up * (height + .22f), new Vector3(width * .72f, .18f, width * .72f), accentMaterial, rotation);
            if (seed % 3 == 0)
            {
                var dome = Sphere(root, "Dome", position + Vector3.up * (height + 1.2f), new Vector3(width * .55f, 2.3f, width * .55f), Material(new Color(.18f, .1f, .16f), .5f, .7f));
                dome.transform.SetParent(root);
                Sphere(root, "Dome Spirit Crown", position + Vector3.up * (height + 2.35f), new Vector3(.42f, .42f, .42f), accentMaterial);
            }
        }

        private static void CreatePyramids(Transform root, Material purple, Material gold)
        {
            var sandGold = Material(new Color(.52f, .28f, .12f), .15f, .4f);
            CreatePyramid(root, new Vector3(-34f, 0f, -16f), 23f, sandGold);
            CreatePyramid(root, new Vector3(-8f, 0f, -22f), 16f, sandGold);
            CreatePyramidCrown(root, new Vector3(-34f, 18.7f, -16f), 5.8f, purple);
            CreatePyramidCrown(root, new Vector3(-8f, 13f, -22f), 4.2f, gold);
        }

        private static void CreatePyramidCrown(Transform root, Vector3 position, float width, Material glow)
        {
            Cube(root, "Pyramid Spirit Crown", position, new Vector3(width, .18f, width), glow, Quaternion.Euler(0f, 45f, 0f));
        }

        private static void CreatePyramid(Transform parent, Vector3 position, float size, Material material)
        {
            var mesh = new Mesh { name = "Procedural Pyramid" };
            var half = size * .5f;
            mesh.vertices = new[] { new Vector3(-half, 0, -half), new Vector3(half, 0, -half), new Vector3(half, 0, half), new Vector3(-half, 0, half), new Vector3(0, size * .8f, 0) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3, 0, 4, 1, 1, 4, 2, 2, 4, 3, 3, 4, 0 };
            mesh.RecalculateNormals();
            var pyramid = new GameObject("Giza Spirit Pyramid");
            pyramid.transform.SetParent(parent);
            pyramid.transform.position = position;
            pyramid.AddComponent<MeshFilter>().sharedMesh = mesh;
            pyramid.AddComponent<MeshRenderer>().material = material;
            pyramid.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        private static void CreateFinishGate(Transform root, Transform start, Material cyan, Material purple, Material gold)
        {
            var right = start.right;
            Cube(root, "Finish Left", start.position - right * 8f + Vector3.up * 3f, new Vector3(.7f, 6f, .7f), gold, start.rotation);
            Cube(root, "Finish Right", start.position + right * 8f + Vector3.up * 3f, new Vector3(.7f, 6f, .7f), purple, start.rotation);
            Cube(root, "Finish Beam", start.position + Vector3.up * 6f, new Vector3(16.7f, .7f, .7f), cyan, start.rotation);
            Cube(root, "Finish Spirit Blade", start.position + Vector3.up * 7.15f, new Vector3(7.5f, .18f, .28f), purple, start.rotation * Quaternion.Euler(0f, 0f, 5f));
            Cube(root, "Finish Gold Blade", start.position + Vector3.up * 7.15f, new Vector3(7.5f, .18f, .28f), gold, start.rotation * Quaternion.Euler(0f, 0f, -5f));
        }

        private static GameObject Cube(Transform parent, string name, Vector3 position, Vector3 scale, Material material, Quaternion rotation, bool local = false)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            if (local) obj.transform.localPosition = position; else obj.transform.position = position;
            obj.transform.rotation = local ? parent.rotation * rotation : rotation;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().material = material;
            return obj;
        }

        private static GameObject Sphere(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = name;
            obj.transform.SetParent(parent);
            obj.transform.position = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().material = material;
            return obj;
        }

        private static Material Material(Color color, float metallic, float smoothness) => RuntimeMaterials.Lit(color, metallic, smoothness);
        private static Material Emissive(Color color, float strength) => RuntimeMaterials.Lit(color, .25f, .85f, strength);
    }
}
