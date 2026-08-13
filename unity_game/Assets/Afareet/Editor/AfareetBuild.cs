using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Afareet.Editor
{
    public static class AfareetBuild
    {
        private const string ScenePath = "Assets/Scenes/Prototype.unity";

        public static void BuildWindows()
        {
            PrepareProject();
            Build(
                BuildTarget.StandaloneWindows64,
                "Builds/Windows/3fareet.exe",
                BuildOptions.Development
            );
        }

        public static void BuildAndroid()
        {
            PrepareProject();
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = false;
            Build(
                BuildTarget.Android,
                "Builds/Android/3fareet-prototype.apk",
                BuildOptions.Development
            );
        }

        private static void PrepareProject()
        {
            PlayerSettings.companyName = "Afareet Studio";
            PlayerSettings.productName = "Afareet Asphalt";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;

            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Afareet Prototype";
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void Build(BuildTarget target, string outputPath, BuildOptions options)
        {
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = target,
                options = options
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException(
                    $"{target} build failed: {report.summary.result} " +
                    $"({report.summary.totalErrors} errors)"
                );

            Debug.Log(
                $"AFAREET_BUILD_SUCCESS target={target} " +
                $"path={Path.GetFullPath(outputPath)} " +
                $"size={report.summary.totalSize}"
            );
        }
    }
}
