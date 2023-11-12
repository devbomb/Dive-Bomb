namespace FastDragon
{
    public static class SpyroConstants
    {
        public const float SpyroUnits = 1f / 500;
        public const float SpyroFrames = 1f / 30;

        public const float Gravity = 12f * (SpyroUnits / (SpyroFrames * SpyroFrames));
        public const float InitialJumpSpeed = (565 * SpyroUnits) / (5 * SpyroFrames);
        public const float InitialJumpDuration = 5 * SpyroFrames;
    }
}