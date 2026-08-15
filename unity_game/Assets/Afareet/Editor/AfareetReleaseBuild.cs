using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Afareet.Editor
{
    public static class AfareetReleaseBuild
    {
        public static void BuildReleaseApk()
        {
            BuildRelease(false);
        }

        public static void BuildReleaseAab()
        {
            BuildRelease(true);
        }

        private static void BuildRelease(bool appBundle)
        {
            AfareetBuild.ConfigureAndroidToolchain();
            AfareetBuild.PrepareProject();
            P1ProductionWorldBuildGate.ValidateAndroidCandidateOrThrow();

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            ValidateSigningConfiguration();

            EditorUserBuildSettings.buildAppBundle = appBundle;
            AfareetBuild.Build(
                BuildTarget.Android,
                appBundle
                    ? "Builds/Android/3fareet-release.aab"
                    : "Builds/Android/3fareet-release.apk",
                BuildOptions.None
            );
        }

        private static void ValidateSigningConfiguration()
        {
            if (!PlayerSettings.Android.useCustomKeystore)
                throw new InvalidOperationException("Android release signing is not configured. Apply the secure signing process before invoking the release builder.");

            if (string.IsNullOrWhiteSpace(PlayerSettings.Android.keystoreName))
                throw new InvalidOperationException("Android release keystore path is missing.");

            if (string.IsNullOrWhiteSpace(PlayerSettings.Android.keyaliasName))
                throw new InvalidOperationException("Android release key alias is missing.");
        }
    }
}
