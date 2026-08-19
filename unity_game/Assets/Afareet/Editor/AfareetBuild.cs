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
            ConfigureAndroidPlayer();
            Build(
                BuildTarget.Android,
                "Builds/Android/afareet-unity3d-debug.apk",
                BuildOptions.None
            );
        }

        /// <summary>
        /// Produces a unified ARM64 development APK from the current code/content snapshot
        /// without claiming production visual or device-evidence readiness. Production
        /// Android remains fail-closed through BuildAndroid().
        /// </summary>
        public static void BuildAndroidExperimental()
        {
            ConfigureAndroidToolchain();
            PrepareProject();
            ConfigureAndroidPlayer();

            using (AfareetBuildContext.BeginExperimentalAndroidBuild())
            {
                Build(
                    BuildTarget.Android,
                    "Builds/Android/afareet-unity3d-experimental.apk",
                    BuildOptions.Development,
                    new[] { "AFAREET_EXPERIMENTAL_APK" }
                );
            }

            Debug.Log("AFAREET_EXPERIMENTAL_APK_READY productionEvidence=false");
        }

        private static void ConfigureAndroidPlayer()
        {
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = false;
        }

        internal static void ConfigureAndroidToolchain()
        {
            var editorDirectory = Path.GetDirectoryName(EditorApplication.applicationPath);
            if (string.IsNullOrWhiteSpace(editorDirectory))
                throw new InvalidOperationException("Unable to resolve the Unity Editor directory.");

            var androidPlayer = Path.Combine(
                editorDirectory,
                "Data", "PlaybackEngines", "AndroidPlayer"
            );

            var configuredSdk = Environment.GetEnvironmentVariable("AFAREET_ANDROID_SDK_ROOT");
            var androidSdk = string.IsNullOrWhiteSpace(configuredSdk)
                ? Path.Combine(androidPlayer, "SDK")
                : Path.GetFullPath(configuredSdk);

            var ndk = Path.Combine(androidPlayer, "NDK");
            var jdk = Path.Combine(androidPlayer, "OpenJDK");
            var cmake = Path.Combine(androidSdk, "cmake", "3.22.1");
            var api36 = Path.Combine(androidSdk, "platforms", "android-36");

            if (!Directory.Exists(androidSdk))
                throw new DirectoryNotFoundException($"Android SDK is missing: {androidSdk}");
            if (!Directory.Exists(ndk))
                throw new DirectoryNotFoundException($"Unity-managed Android NDK is missing: {ndk}");
            if (!Directory.Exists(jdk))
                throw new DirectoryNotFoundException($"Unity-managed OpenJDK is missing: {jdk}");
            if (!Directory.Exists(cmake))
                throw new DirectoryNotFoundException($"Android CMake 3.22.1 is missing: {cmake}");
            if (!Directory.Exists(api36))
                throw new DirectoryNotFoundException(
                    $"Android API 36 platform is missing from the selected SDK: {api36}"
                );

            AndroidExternalToolsSettings.sdkRootPath = androidSdk;
            AndroidExternalToolsSettings.ndkRootPath = ndk;
            AndroidExternalToolsSettings.jdkRootPath = jdk;

            Debug.Log(
                $"AFAREET_ANDROID_TOOLCHAIN sdk={androidSdk} ndk={ndk} jdk={jdk} " +
                "targetApi=36 source=unity-hub-managed"
            );
        }

        internal static void PrepareProject()
        {
            AfareetAssetSetup.EnsureConfigAssets();
            P1ProductionWorldAssetStager.StageTrackedSourcesOrThrow();

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

        internal static void Build(
            BuildTarget target,
            string outputPath,
            BuildOptions options,
            string[] extraScriptingDefines = null
        )
        {
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = target,
                options = options,
                extraScriptingDefines = extraScriptingDefines ?? Array.Empty<string>()
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
