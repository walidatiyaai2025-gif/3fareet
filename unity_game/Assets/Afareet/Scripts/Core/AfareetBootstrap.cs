using Afareet.CareerRuntime;
using Afareet.Race;
using Afareet.UI;
using Afareet.Vehicle;
using Afareet.World;
using UnityEngine;

namespace Afareet.Core
{
    public sealed class AfareetBootstrap : MonoBehaviour
    {
        private const string RootName = "AFAREET_RUNTIME";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateRuntime()
        {
            if (GameObject.Find(RootName) != null) return;

            ConfigureApplicationRuntime();

            var root = new GameObject(RootName);
            DontDestroyOnLoad(root);
            root.AddComponent<AfareetBootstrap>().Build();
        }

        private void Build()
        {
            var vehicleConfig = LoadConfig<ArcadeCarConfig>("Config/ArcadeCarConfig");
            var cameraConfig = LoadConfig<ChaseCameraConfig>("Config/ChaseCameraConfig");

            SetupLighting();
            var track = CairoTrackBuilder.Build(transform);
            var player = CarFactory.CreatePlayer(track.StartPosition, track.StartRotation, transform, vehicleConfig);

            var race = gameObject.AddComponent<RaceDirector>();
            race.Configure(player, track);
            var performance = gameObject.AddComponent<RacePerformanceMetricsTracker>();
            performance.Configure(player, race);

            for (var i = 0; i < 3; i++)
            {
                var ai = CarFactory.CreateRival(i, track.GridPosition(i + 1), track.StartRotation, transform, vehicleConfig);
                ai.gameObject.AddComponent<AiRacer>().Configure(track.Waypoints, i);
                race.RegisterRival(ai);
            }

            var career = gameObject.AddComponent<CareerGameSession>();
            career.Configure(
                player.GetComponent<RaceRoundController>(),
                race,
                performance,
                new PlayerPrefsCareerProgressStorage());

            CreateCamera(player.transform, cameraConfig);
            InstallRuntimeUi(player, race, career);
            gameObject.AddComponent<UnitySplashOverlay>();
        }

        private static void ConfigureApplicationRuntime()
        {
            Application.targetFrameRate = 60;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
            QualitySettings.vSyncCount = 0;
        }

        private void InstallRuntimeUi(
            ArcadeCarController player,
            RaceDirector race,
            CareerGameSession career)
        {
            var input = gameObject.AddComponent<ProductionRaceInputController>();
            input.Configure(player, race);

            var controls = ProductionRaceControlsOverlay.EnsureInstalled(transform);
            controls.Configure(race, input);

            var productionHud = ProductionRaceHud.EnsureInstalled(transform);
            productionHud.Configure(player, race, career);

            var flowOverlay = ProductionRaceFlowOverlay.EnsureInstalled(transform);
            flowOverlay.Configure(race, career);
        }

        private static void SetupLighting()
        {
            RenderSettings.ambientLight = new Color(0.12f, 0.14f, 0.28f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.025f, 0.035f, 0.09f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.006f;

            var moon = new GameObject("Moon Light").AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = new Color(0.45f, 0.62f, 1f);
            moon.intensity = 1.15f;
            moon.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
            moon.shadows = LightShadows.Soft;
        }

        private static void CreateCamera(Transform target, ChaseCameraConfig config)
        {
            var cameraObject = new GameObject("Racing Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 65f;
            camera.nearClipPlane = 0.15f;
            camera.farClipPlane = 850f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.012f, 0.018f, 0.055f);
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<ChaseCamera>().Configure(target, config);
        }

        private static T LoadConfig<T>(string resourcePath) where T : ScriptableObject
        {
            var config = Resources.Load<T>(resourcePath);
            if (config == null)
                throw new MissingReferenceException($"Required config is missing at Resources/{resourcePath}.");
            return config;
        }
    }
}
