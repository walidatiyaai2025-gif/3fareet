namespace Afareet.Support
{
    public static class LayoutSeedPolicy
    {
        public static int Slot(int seed, int index, int slotCount)
        {
            if (slotCount <= 0) return 0;
            unchecked
            {
                var value = seed * 1103515245 + 12345 + index * 97;
                return (value & 0x7fffffff) % slotCount;
            }
        }
    }
}
