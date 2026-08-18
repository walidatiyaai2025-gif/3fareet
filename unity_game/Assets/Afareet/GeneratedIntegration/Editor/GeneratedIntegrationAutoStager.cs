#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Afareet.GeneratedIntegration.Editor
{
    public static class GeneratedIntegrationAutoStager
    {
        private const string Root = "Assets/Afareet/GeneratedIntegration";
        private const string VehicleRoot = Root + "/Vehicles";
        private const string PrefabRoot = Root + "/Prefabs";
        private const string SceneRoot = Root + "/Scenes";
        private const string ReportPath = Root + "/IntegrationStageReport.json";

        private static readonly string[] VehicleIds =
        {
            "AfareetKing",
            "Rival01_Wedge",
            "Rival02_Muscle",
            "Rival03_Prototype"
        };

        private static readonly float[] LodHeights = { 0.60f, 0.30f, 0.10f };

        public static void Stage()
        {
            try
            {
                Debug.Log("AFAREET_GENERATED_INTEGRATION_STAGE_START");

                EnsureFolder(Root, "Prefabs");
                EnsureFolder(Root, "Scenes");

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                var prefabs = VehicleIds
                    .Select(BuildVehiclePrefab)
                    .ToArray();

                BuildReviewScene(prefabs);
                WriteReport(prefabs);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                Debug.Log(
                    "AFAREET_GENERATED_INTEGRATION_STAGE_OK " +
                    "classification=GENERATED_INTEGRATION_CANDIDATE " +
                    "vehicles=4 scene=SCN_3FAREET_GeneratedIntegration"
                );
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                throw;
            }
        }

        private static GameObject BuildVehiclePrefab(string vehicleId)
        {
            var root = new GameObject("PF_" + vehicleId + "_GeneratedIntegration");
            var lods = new LOD[3];

            try
            {
                for (var i = 0; i < 3; i++)
                {
                    var lodName = "LOD" + i;
                    var modelPath = VehicleRoot + "/" + vehicleId + "/" + vehicleId + "_" + lodName + ".fbx";
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);

                    if (model == null)
                    {
                        throw new InvalidOperationException("Missing imported model: " + modelPath);
                    }

                    var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
                    if (instance == null)
                    {
                        instance = UnityEngine.Object.Instantiate(model);
                    }

                    instance.name = vehicleId + "_" + lodName;
                    instance.transform.SetParent(root.transform, false);

                    var renderers = instance.GetComponentsInChildren<Renderer>(true);
                    if (renderers.Length == 0)
                    {
                        throw new InvalidOperationException("No renderers in " + modelPath);
                    }

                    lods[i] = new LOD(LodHeights[i], renderers);
                }

                var group = root.AddComponent<LODGroup>();
                group.SetLODs(lods);
                group.RecalculateBounds();

                var prefabPath = PrefabRoot + "/PF_" + vehicleId + "_GeneratedIntegration.prefab";
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

                if (prefab == null)
                {
                    throw new InvalidOperationException("Failed to save prefab: " + prefabPath);
                }

                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildReviewScene(GameObject[] vehiclePrefabs)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var environmentPath =
                Root + "/Environment/CairoNight/CairoNight_Environment_Master.fbx";

            var environment =
                AssetDatabase.LoadAssetAtPath<GameObject>(environmentPath);

            if (environment == null)
            {
                throw new InvalidOperationException(
                    "Missing Cairo Night environment: " + environmentPath
                );
            }

            var envInstance = PrefabUtility.InstantiatePrefab(environment) as GameObject;
            if (envInstance == null)
            {
                envInstance = UnityEngine.Object.Instantiate(environment);
            }
            envInstance.name = "ENV_CairoNight_GeneratedIntegration";

            var positions = new[]
            {
                new Vector3(0.0f, 0.0f, -31.2f),
                new Vector3(-2.25f, 0.0f, -34.0f),
                new Vector3(2.25f, 0.0f, -34.0f),
                new Vector3(0.0f, 0.0f, -36.6f)
            };

            for (var i = 0; i < vehiclePrefabs.Length; i++)
            {
                var car = PrefabUtility.InstantiatePrefab(vehiclePrefabs[i]) as GameObject;
                if (car == null)
                {
                    car = UnityEngine.Object.Instantiate(vehiclePrefabs[i]);
                }

                car.name = vehiclePrefabs[i].name;
                car.transform.position = positions[i];
                car.transform.rotation = Quaternion.identity;
            }

            var key = new GameObject("GeneratedIntegration_KeyLight");
            var keyLight = key.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.35f;
            key.transform.rotation = Quaternion.Euler(42f, -28f, 0f);

            var fill = new GameObject("GeneratedIntegration_FillLight");
            var fillLight = fill.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.intensity = 0.55f;
            fillLight.color = new Color(0.45f, 0.55f, 1.0f);
            fill.transform.rotation = Quaternion.Euler(58f, 145f, 0f);

            var cameraObject = new GameObject("GeneratedIntegration_Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.fieldOfView = 62f;
            cameraObject.transform.position = new Vector3(0f, 3.4f, -40.5f);
            cameraObject.transform.LookAt(new Vector3(0f, 1.0f, -22f));

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.06f, 0.07f, 0.12f);

            var scenePath = SceneRoot + "/SCN_3FAREET_GeneratedIntegration.unity";
            if (!EditorSceneManager.SaveScene(scene, scenePath))
            {
                throw new InvalidOperationException("Failed to save scene: " + scenePath);
            }
        }

        private static void WriteReport(GameObject[] prefabs)
        {
            var projectParent = Directory.GetParent(Application.dataPath);
            if (projectParent == null) throw new InvalidOperationException("Unable to resolve Unity project root.");
            var projectRoot = projectParent.FullName;
            var absolute = Path.Combine(
                projectRoot,
                ReportPath.Replace('/', Path.DirectorySeparatorChar)
            );

            var json =
                "{\n" +
                "  \"schemaVersion\": 1,\n" +
                "  \"classification\": \"GENERATED_INTEGRATION_CANDIDATE\",\n" +
                "  \"productionAccepted\": false,\n" +
                "  \"verified\": false,\n" +
                "  \"vehiclePrefabs\": 4,\n" +
                "  \"scene\": \"Assets/Afareet/GeneratedIntegration/Scenes/SCN_3FAREET_GeneratedIntegration.unity\",\n" +
                "  \"note\": \"Technical/programmer integration only; does not satisfy UART-003/004/005/006/007, URAC-011 or UPER-009 production acceptance.\"\n" +
                "}\n";

            File.WriteAllText(absolute, json);
        }

        private static void EnsureFolder(string parent, string child)
        {
            var target = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(target))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
#endif
