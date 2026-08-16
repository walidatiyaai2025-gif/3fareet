using System;

namespace Afareet.Editor
{
    /// <summary>
    /// Process-local build context used only while BuildPipeline.BuildPlayer is executing.
    /// Production builds remain fail-closed; the experimental Android scope is explicit,
    /// non-nestable, internal to the editor assembly, and reset through IDisposable/finally semantics.
    /// </summary>
    internal static class AfareetBuildContext
    {
        internal static bool IsExperimentalAndroidBuild { get; private set; }

        internal static IDisposable BeginExperimentalAndroidBuild()
        {
            if (IsExperimentalAndroidBuild)
                throw new InvalidOperationException("An experimental Android build scope is already active.");

            IsExperimentalAndroidBuild = true;
            return new ExperimentalAndroidScope();
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
