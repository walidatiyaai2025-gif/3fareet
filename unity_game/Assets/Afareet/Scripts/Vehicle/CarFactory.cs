using UnityEngine;
using Afareet.World;

namespace Afareet.Vehicle
{
    public static class CarFactory
    {
        private static readonly Color[] RivalColors =
        {
            new Color(1f, .2f, .62f), new Color(1f, .62f, .12f), new Color(.35f, 1f, .42f)
        };

        public static ArcadeCarController CreatePlayer(Vector3 position, Quaternion rotation, Transform parent, ArcadeCarConfig config)
        {
            var car = Create("PLAYER — عفريت", position, rotation, new Color(.08f, .83f, 1f), parent, config);
            car.AcceptsPlayerInput = true;
            return car;
        }

        public static ArcadeCarController CreateRival(int index, Vector3 position, Quaternion rotation, Transform parent, ArcadeCarConfig config)
        {
            return Create($"RIVAL {index + 1}", position, rotation, RivalColors[index % RivalColors.Length], parent, config);
        }

        private static ArcadeCarController Create(string name, Vector3 position, Quaternion rotation, Color accent, Transform parent, ArcadeCarConfig config)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.SetPositionAndRotation(position, rotation);
            var body = root.AddComponent<Rigidbody>();
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.9f, 1.05f, 4.1f);
            collider.center = new Vector3(0f, .62f, 0f);

            CreatePart(root.transform, "Body", PrimitiveType.Cube, new Vector3(1.9f, .55f, 4.1f), new Vector3(0f, .55f, 0f), accent);
            CreatePart(root.transform, "Cabin", PrimitiveType.Cube, new Vector3(1.45f, .65f, 1.85f), new Vector3(0f, 1.05f, -.25f), new Color(.025f, .05f, .1f));
            CreatePart(root.transform, "Spirit Hood", PrimitiveType.Sphere, new Vector3(1.35f, .16f, 1.2f), new Vector3(0f, .88f, 1.05f), accent * 1.4f);

            for (var side = -1; side <= 1; side += 2)
            for (var axle = -1; axle <= 1; axle += 2)
            {
                var wheel = CreatePart(root.transform, "Wheel", PrimitiveType.Cylinder, new Vector3(.72f, .32f, .72f), new Vector3(side * 1.02f, .38f, axle * 1.35f), Color.black);
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }

            var underglow = new GameObject("Spirit Underglow").AddComponent<Light>();
            underglow.transform.SetParent(root.transform, false);
            underglow.transform.localPosition = new Vector3(0f, .15f, 0f);
            underglow.type = LightType.Point;
            underglow.color = accent;
            underglow.range = 5f;
            underglow.intensity = 4f;

            CreateTrail(root.transform, new Vector3(-.62f, .2f, -1.9f), accent);
            CreateTrail(root.transform, new Vector3(.62f, .2f, -1.9f), accent);
            _ = body;
            var controller = root.AddComponent<ArcadeCarController>();
            controller.Configure(config);
            return controller;
        }

        private static GameObject CreatePart(Transform parent, string name, PrimitiveType type, Vector3 scale, Vector3 position, Color color)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            Object.Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            var material = RuntimeMaterials.Lit(color, .65f, .8f);
            part.GetComponent<Renderer>().material = material;
            return part;
        }

        private static void CreateTrail(Transform parent, Vector3 position, Color color)
        {
            var trail = new GameObject("Spirit Trail").AddComponent<TrailRenderer>();
            trail.transform.SetParent(parent, false);
            trail.transform.localPosition = position;
            trail.time = .55f;
            trail.startWidth = .18f;
            trail.endWidth = 0f;
            trail.material = RuntimeMaterials.Trail(color);
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
            trail.emitting = false;
        }
    }
}
