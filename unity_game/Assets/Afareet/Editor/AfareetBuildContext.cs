using System;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Afareet.Editor
{
    /// <summary>
    /// Process-local build context used only while BuildPipeline.BuildPlayer is executing.
    /// Production builds remain fail-closed; the experimental Android scope is explicit,
    /// non-nestable, internal to the editor assembly, and reset through IDisposable/finally semantics.
    /// </summary>
    internal static class AfareetBuildContext
    {
        internal const string ExperimentalAndroidOutput =
            "Builds/Android/afareet-unity3d-experimental.apk";

        internal static bool IsExperimentalAndroidBuild { get; private set; }

        internal static IDisposable BeginExperimentalAndroidBuild()
        {
            if (IsExperimentalAndroidBuild)
                throw new InvalidOperationException("An experimental Android build scope is already active.");

            IsExperimentalAndroidBuild = true;
            return new ExperimentalAndroidScope();
        }

        /// <summary>
        /// Returns true only for the explicit experimental Android BuildPipeline invocation.
        /// A context flag alone is intentionally insufficient: the report must also be an
        /// Android Development build targeting the dedicated experimental APK output.
        /// </summary>
        internal static bool IsDedicatedExperimentalAndroidBuild(BuildReport report)
        {
            if (!IsExperimentalAndroidBuild || report == null)
                return false;
            if (report.summary.platform != BuildTarget.Android)
                return false;
            if ((report.summary.options & BuildOptions.Development) == 0)
                return false;

            var normalizedOutput = (report.summary.outputPath ?? string.Empty).Replace('\\', '/');
            return normalizedOutput.EndsWith(
                ExperimentalAndroidOutput,
                StringComparison.OrdinalIgnoreCase
            );
        }

        private sealed class ExperimentalAndroidScope : IDisposable
        {
            private bool disposed;

            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                IsExperimentalAndroidBuild = false;
            }
        }
    }
}
