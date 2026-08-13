namespace Afareet.Support
{
    public enum GameplaySfxKind { Drift, Nitro, Impact, Ui }

    public static class GameplaySfxMixPolicy
    {
        public static int Priority(GameplaySfxKind kind)
        {
            switch (kind)
            {
                case GameplaySfxKind.Impact: return 100;
                case GameplaySfxKind.Nitro: return 80;
                case GameplaySfxKind.Drift: return 60;
                default: return 40;
            }
        }

        public static float CooldownSeconds(GameplaySfxKind kind)
        {
            return kind == GameplaySfxKind.Impact ? .12f : kind == GameplaySfxKind.Ui ? .08f : .2f;
        }
    }
}
