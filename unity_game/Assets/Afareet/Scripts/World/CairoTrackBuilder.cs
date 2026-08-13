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
            var root = new GameObject("CAIRO NEON CIRCUIT").transform;
            root.SetParent(parent);
            CreateGround(root);

            var asphalt = Material(new Color(.035f, .045f, .075f), .1f, .55f);
            var cyan = Emissive(new Color(0f, .72f, 1f), 4f);
            var gold = Emissive(new Color(1f, .48f, .06f), 3f);

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
                CreateNeonRail(root, p + right * (RoadWidth * .53f), rotation, length, i % 2 == 0 ? cyan : gold);
                CreateNeonRail(root, p - right * (RoadWidth * .53f), rotation, length, i % 2 == 0 ? gold : cyan);

                if (i % 4 == 0) CreateBuilding(root, p + right * (RoadWidth + 8f), i, gold);
                if (i % 5 == 0) CreateBuilding(root, p - right * (RoadWidth + 10f), i + 17, cyan);
            }

            CreatePyramids(root);
            CreateFinishGate(root, track.Waypoints[0], cyan, gold);
            return track;
        }

        private static Vector3 Point(float t) => new(RadiusX * Mathf.Cos(t), 0f, RadiusZ * Mathf.Sin(t));

        private static void CreateGround(Transform root)
        {
            var ground = Cube(root, "Desert Ground", Vector3.down * .35f, new Vector3(260f, .5f, 210f), Material(new Color(.075f, .055f, .065f), 0f, .1f), Quaternion.identity);
            ground.GetComponent<Collider>().isTrigger = false;
        }

        private static void CreateNeonRail(Transform root, Vector3 position, Quaternion rotation, float length, Material glow)
        {
            Cube(root, "Neon Rail", position + Vector3.up * .35f, new Vector3(.18f, .18f, length), glow, rotation);
        }

        private static void CreateBuilding(Transform root, Vector3 position, int seed, Material windowMaterial)
        {
            var height = 8f + (seed * 7 % 17);
            var width = 5f + (seed * 3 % 6);
            var building = Cube(root, "Cairo Building", position + Vector3.up * height * .5f, new Vector3(width, height, width), Material(new Color(.08f, .055f, .09f), .15f, .35f), Quaternion.Euler(0f, seed * 31f, 0f));
            for (var floor = 2; floor < height - 1; floor += 3)
                Cube(building.transform, "Warm Window", new Vector3(0f, floor - height * .5f, -width * .505f), new Vector3(width * .55f, .5f, .05f), windowMaterial, Quaternion.identity, true);

            if (seed % 3 == 0)
            {
                var dome = Sphere(root, "Dome", position + Vector3.up * (height + 1.2f), new Vector3(width * .55f, 2.3f, width * .55f), Material(new Color(.18f, .1f, .16f), .5f, .7f));
                dome.transform.SetParent(root);
            }
        }

        private static void CreatePyramids(Transform root)
        {
            var sandGold = Material(new Color(.52f, .28f, .12f), .15f, .4f);
            CreatePyramid(root, new Vector3(-34f, 0f, -16f), 23f, sandGold);
            CreatePyramid(root, new Vector3(-8f, 0f, -22f), 16f, sandGold);
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

        private static void CreateFinishGate(Transform root, Transform start, Material cyan, Material gold)
        {
            var right = start.right;
            Cube(root, "Finish Left", start.position - right * 8f + Vector3.up * 3f, new Vector3(.7f, 6f, .7f), gold, start.rotation);
            Cube(root, "Finish Right", start.position + right * 8f + Vector3.up * 3f, new Vector3(.7f, 6f, .7f), cyan, start.rotation);
            Cube(root, "Finish Beam", start.position + Vector3.up * 6f, new Vector3(16.7f, .7f, .7f), cyan, start.rotation);
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

        private static Material Material(Color color, float metallic, float smoothness)
        {
            return RuntimeMaterials.Lit(color, metallic, smoothness);
        }

        private static Material Emissive(Color color, float strength)
        {
            return RuntimeMaterials.Lit(color, .25f, .85f, strength);
        }
    }
}
