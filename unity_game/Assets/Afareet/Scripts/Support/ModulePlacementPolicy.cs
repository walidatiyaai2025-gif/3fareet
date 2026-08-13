namespace Afareet.Support
{
    public static class ModulePlacementPolicy
    {
        public static float ForwardOffset(int index, float spacing)
        {
            if (index < 0 || spacing <= 0f) return 0f;
            return index * spacing;
        }

        public static float SideOffset(int index, float edge, float clearance)
        {
            var sign = index % 2 == 0 ? -1f : 1f;
            return sign * (System.Math.Abs(edge) + System.Math.Max(0f, clearance));
        }

        public static float Yaw(int index) => index % 2 == 0 ? 90f : -90f;
    }
}
