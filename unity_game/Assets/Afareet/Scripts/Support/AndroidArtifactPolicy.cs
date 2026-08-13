namespace Afareet.Support
{
    public static class AndroidArtifactPolicy
    {
        public static bool Accept(string packageId, string label, bool arm64, bool landscape)
        {
            return packageId == "com.fiftysolutions.afareetunity3d"
                && label == "3Fareet"
                && arm64
                && landscape;
        }
    }
}
