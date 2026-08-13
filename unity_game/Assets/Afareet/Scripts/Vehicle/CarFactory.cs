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
            var car = Create("PLAYER HERO — AFAREET", position, rotation, new Color(.38f, .02f, .72f), parent, config, true);
            car.AcceptsPlayerInput = true;
            return car;
        }

        public static ArcadeCarController CreateRival(int index, Vector3 position, Quaternion rotation, Transform parent, ArcadeCarConfig config)
        {
            return Create($"RIVAL {index + 1}", position, rotation, RivalColors[index % RivalColors.Length], parent, config, false);
        }

        private static ArcadeCarController Create(string name, Vector3 position, Quaternion rotation, Color accent, Transform parent, ArcadeCarConfig config, bool isHero)
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
            if (isHero) CreateHeroDetails(root.transform, accent);

            for (var side = -1; side <= 1; side += 2)
            for (var axle = -1; axle <= 1; axle += 2)
            {
                CreateWheel(root.transform, side, axle, isHero);
            }

            var underglow = new GameObject("Spirit Underglow").AddComponent<Light>();
            underglow.transform.SetParent(root.transform, false);
            underglow.transform.localPosition = new Vector3(0f, .15f, 0f);
            underglow.type = LightType.Point;
            underglow.color = accent;
            underglow.range = isHero ? 6.5f : 5f;
            underglow.intensity = isHero ? 5.2f : 4f;

            CreateTrail(root.transform, new Vector3(-.62f, .2f, -1.9f), accent, isHero ? .24f : .18f);
            CreateTrail(root.transform, new Vector3(.62f, .2f, -1.9f), accent, isHero ? .24f : .18f);
            _ = body;
            var controller = root.AddComponent<ArcadeCarController>();
            controller.Configure(config);
            return controller;
        }

        private static void CreateHeroDetails(Transform root, Color bodyAccent)
        {
            var gold = new Color(1f, .48f, .035f);
            var purple = new Color(.52f, .02f, 1f);
            var black = new Color(.008f, .009f, .018f);
            var glass = new Color(.015f, .12f, .19f);
            var white = new Color(.88f, .96f, 1f);
            var red = new Color(1f, .03f, .08f);

            // Lower/wider Egyptian street-racer silhouette.
            CreatePart(root, "Hero Front Bumper", PrimitiveType.Cube, new Vector3(2.02f, .30f, .34f), new Vector3(0f, .44f, 2.03f), bodyAccent * .9f);
            CreatePart(root, "Hero Rear Bumper", PrimitiveType.Cube, new Vector3(2.00f, .27f, .30f), new Vector3(0f, .46f, -2.03f), bodyAccent * .82f);
            CreatePart(root, "Hero Front Splitter", PrimitiveType.Cube, new Vector3(2.20f, .10f, .55f), new Vector3(0f, .25f, 2.16f), black);
            CreatePart(root, "Hero Splitter Gold Lip", PrimitiveType.Cube, new Vector3(2.28f, .045f, .58f), new Vector3(0f, .20f, 2.18f), gold);
            CreatePart(root, "Hero Left Side Skirt", PrimitiveType.Cube, new Vector3(.14f, .16f, 3.28f), new Vector3(-1.00f, .28f, -.10f), gold);
            CreatePart(root, "Hero Right Side Skirt", PrimitiveType.Cube, new Vector3(.14f, .16f, 3.28f), new Vector3(1.00f, .28f, -.10f), gold);

            // Hood and supernatural identity.
            CreatePart(root, "Hero Gold Hood Stripe", PrimitiveType.Cube, new Vector3(.27f, .035f, 3.58f), new Vector3(0f, .86f, .18f), gold);
            CreatePart(root, "Hero Left Hood Rune", PrimitiveType.Cube, new Vector3(.10f, .025f, 1.48f), new Vector3(-.52f, .89f, .86f), purple)
                .transform.localRotation = Quaternion.Euler(0f, -13f, 0f);
            CreatePart(root, "Hero Right Hood Rune", PrimitiveType.Cube, new Vector3(.10f, .025f, 1.48f), new Vector3(.52f, .89f, .86f), purple)
                .transform.localRotation = Quaternion.Euler(0f, 13f, 0f);
            CreatePart(root, "Hero Blower", PrimitiveType.Cube, new Vector3(.92f, .38f, .82f), new Vector3(0f, 1.22f, .72f), black);
            for (var intake = -1; intake <= 1; intake++)
                CreatePart(root, "Purple Intake", PrimitiveType.Cylinder, new Vector3(.2f, .32f, .2f), new Vector3(intake * .28f, 1.38f, .88f), purple)
                    .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // Glass treatment breaks up the blockout cabin and improves readability from chase camera.
            CreatePart(root, "Hero Windshield", PrimitiveType.Cube, new Vector3(1.20f, .46f, .055f), new Vector3(0f, 1.14f, .72f), glass)
                .transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
            CreatePart(root, "Hero Rear Glass", PrimitiveType.Cube, new Vector3(1.16f, .40f, .055f), new Vector3(0f, 1.12f, -1.18f), glass)
                .transform.localRotation = Quaternion.Euler(-18f, 0f, 0f);
            CreatePart(root, "Hero Left Window", PrimitiveType.Cube, new Vector3(.045f, .40f, 1.14f), new Vector3(-.755f, 1.11f, -.28f), glass);
            CreatePart(root, "Hero Right Window", PrimitiveType.Cube, new Vector3(.045f, .40f, 1.14f), new Vector3(.755f, 1.11f, -.28f), glass);

            // Aggressive face: spirit eyes, grille and fangs.
            CreatePart(root, "Hero Grille", PrimitiveType.Cube, new Vector3(1.10f, .30f, .075f), new Vector3(0f, .50f, 2.225f), black);
            CreatePart(root, "Left Spirit Eye", PrimitiveType.Sphere, new Vector3(.58f, .18f, .12f), new Vector3(-.58f, .70f, 2.10f), purple);
            CreatePart(root, "Right Spirit Eye", PrimitiveType.Sphere, new Vector3(.58f, .18f, .12f), new Vector3(.58f, .70f, 2.10f), purple);
            CreatePart(root, "Left Headlight Core", PrimitiveType.Sphere, new Vector3(.23f, .10f, .09f), new Vector3(-.58f, .70f, 2.17f), white);
            CreatePart(root, "Right Headlight Core", PrimitiveType.Sphere, new Vector3(.23f, .10f, .09f), new Vector3(.58f, .70f, 2.17f), white);
            CreatePart(root, "Left Fang", PrimitiveType.Cube, new Vector3(.14f, .34f, .12f), new Vector3(-.38f, .42f, 2.20f), Color.white)
                .transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            CreatePart(root, "Right Fang", PrimitiveType.Cube, new Vector3(.14f, .34f, .12f), new Vector3(.38f, .42f, 2.20f), Color.white)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 18f);

            // Rear signature for overtakes and result shots.
            CreatePart(root, "Hero Spoiler", PrimitiveType.Cube, new Vector3(2.58f, .15f, .62f), new Vector3(0f, 1.48f, -1.92f), purple);
            CreatePart(root, "Spoiler Left Support", PrimitiveType.Cube, new Vector3(.13f, .58f, .16f), new Vector3(-.82f, 1.18f, -1.72f), gold);
            CreatePart(root, "Spoiler Right Support", PrimitiveType.Cube, new Vector3(.13f, .58f, .16f), new Vector3(.82f, 1.18f, -1.72f), gold);
            CreatePart(root, "Left Tail Light", PrimitiveType.Cube, new Vector3(.52f, .16f, .08f), new Vector3(-.58f, .68f, -2.10f), red);
            CreatePart(root, "Right Tail Light", PrimitiveType.Cube, new Vector3(.52f, .16f, .08f), new Vector3(.58f, .68f, -2.10f), red);
            CreatePart(root, "Left Exhaust", PrimitiveType.Cylinder, new Vector3(.19f, .34f, .19f), new Vector3(-.62f, .28f, -2.15f), black)
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            CreatePart(root, "Right Exhaust", PrimitiveType.Cylinder, new Vector3(.19f, .34f, .19f), new Vector3(.62f, .28f, -2.15f), black)
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            CreateHeroAccentLight(root, "Left Spirit Lamp", new Vector3(-.58f, .69f, 2.16f), purple);
            CreateHeroAccentLight(root, "Right Spirit Lamp", new Vector3(.58f, .69f, 2.16f), purple);
        }

        private static void CreateWheel(Transform root, int side, int axle, bool isHero)
        {
            var wheel = CreatePart(root, "Wheel", PrimitiveType.Cylinder, new Vector3(.72f, .32f, .72f), new Vector3(side * 1.02f, .38f, axle * 1.35f), Color.black);
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            if (!isHero) return;

            var gold = new Color(1f, .48f, .035f);
            var purple = new Color(.52f, .02f, 1f);
            var rim = CreatePart(root, "Hero Rim", PrimitiveType.Cylinder, new Vector3(.46f, .34f, .46f), new Vector3(side * 1.025f, .38f, axle * 1.35f), gold);
            rim.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            var hub = CreatePart(root, "Hero Spirit Hub", PrimitiveType.Cylinder, new Vector3(.18f, .36f, .18f), new Vector3(side * 1.03f, .38f, axle * 1.35f), purple);
            hub.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        private static void CreateHeroAccentLight(Transform root, string name, Vector3 localPosition, Color color)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.transform.SetParent(root, false);
            light.transform.localPosition = localPosition;
            light.type = LightType.Point;
            light.color = color;
            light.range = 3.2f;
            light.intensity = 2.2f;
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

        private static void CreateTrail(Transform parent, Vector3 position, Color color, float width)
        {
            var trail = new GameObject("Spirit Trail").AddComponent<TrailRenderer>();
            trail.transform.SetParent(parent, false);
            trail.transform.localPosition = position;
            trail.time = .55f;
            trail.startWidth = width;
            trail.endWidth = 0f;
            trail.material = RuntimeMaterials.Trail(color);
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
            trail.emitting = false;
        }
    }
}
