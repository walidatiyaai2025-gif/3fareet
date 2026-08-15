using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Android;
using UnityEngine;

namespace Afareet.Editor
{
    public static class AfareetBuild
    {
        private const string ScenePath = "Assets/Scenes/Prototype.unity";
        private const string IconPath = "Assets/Afareet/Branding/afareet_app_icon.png";

        public static void BuildWindows()
        {
            PrepareProject();
            Build(
                BuildTarget.StandaloneWindows64,
                "Builds/Windows/afareet-unity3d.exe",
                BuildOptions.Development
            );
        }

        public static void BuildAndroid()
        {
            ConfigureAndroidToolchain();
            PrepareProject();
            P1ProductionWorldBuildGate.ValidateAndroidCandidateOrThrow();
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = false;
            Build(
                BuildTarget.Android,
                "Builds/Android/afareet-unity3d-debug.apk",
                BuildOptions.None
            );
        }

        internal static void ConfigureAndroidToolchain()
        {
            var configuredSdk = Environment.GetEnvironmentVariable("AFAREET_ANDROID_SDK_ROOT");
            var androidSdk = string.IsNullOrWhiteSpace(configuredSdk)
                ? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Android",
                    "Sdk"
                )
                : configuredSdk;

            if (!Directory.Exists(Path.Combine(androidSdk, "cmake", "3.22.1")))
                return;

            AndroidExternalToolsSettings.sdkRootPath = androidSdk;
            Debug.Log($"AFAREET_ANDROID_SDK path={androidSdk}");
        }

        internal static void PrepareProject()
        {
            AfareetAssetSetup.EnsureConfigAssets();
            PlayerSettings.companyName = "Afareet Studio";
            PlayerSettings.productName = "Afareet Asphalt Unity3D";
            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.Android,
                "com.fiftysolutions.afareetunity3d"
            );
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon == null)
                throw new InvalidOperationException($"App icon is missing at {IconPath}");
            ApplyPlatformIcons(NamedBuildTarget.Android, icon);
            ApplyPlatformIcons(NamedBuildTarget.Standalone, icon);
            PlayerSettings.SetIcons(
                NamedBuildTarget.Android,
                new[] { icon },
                IconKind.Application
            );
            PlayerSettings.SetIcons(
                NamedBuildTarget.Standalone,
                new[] { icon },
                IconKind.Any
            );
#pragma warning disable CS0618
            PlayerSettings.SetIconsForTargetGroup(
                BuildTargetGroup.Standalone,
                new[] { icon }
            );
            PlayerSettings.SetIconsForTargetGroup(
                BuildTargetGroup.Android,
                new[] { icon }
            );
#pragma warning restore CS0618

            // The tracked prototype scene is production source input. A build must
            // never regenerate or resave it: doing so makes an otherwise successful
            // exact-SHA build dirty and invalidates release evidence. Treat a missing
            // or invalid scene as a hard configuration error instead.
            var prototypeScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (prototypeScene == null)
                throw new InvalidOperationException($"Build scene is missing or invalid at {ScenePath}");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ApplyPlatformIcons(NamedBuildTarget target, Texture2D icon)
        {
            foreach (var kind in PlayerSettings.GetSupportedIconKinds(target))
            {
                var platformIcons = PlayerSettings.GetPlatformIcons(target, kind);
                foreach (var platformIcon in platformIcons)
                {
                    for (var layer = 0; layer < platformIcon.maxLayerCount; layer++)
                        platformIcon.SetTexture(icon, layer);
                }
                PlayerSettings.SetPlatformIcons(target, kind, platformIcons);
            }
        }

        internal static void Build(BuildTarget target, string outputPath, BuildOptions options)
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
