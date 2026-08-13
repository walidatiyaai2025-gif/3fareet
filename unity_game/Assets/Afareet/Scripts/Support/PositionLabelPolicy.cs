namespace Afareet.Support
{
    public static class PositionLabelPolicy
    {
        public static string Format(int position, int total)
        {
            if (total < 1) total = 1;
            if (position < 1) position = 1;
            if (position > total) position = total;
            return position + "/" + total;
        }
    }
}
