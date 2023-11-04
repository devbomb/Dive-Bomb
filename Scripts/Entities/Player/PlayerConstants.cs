using Godot;

namespace FastDragon
{
    public partial class Player
    {
        /// <summary>
        /// Conversion factor between Godot distance units and Spyro distance
        /// units.
        ///
        /// The player in this game has a diameter of 1 Godot unit.
        /// I measured Spyro's diameter to be about 500 Spyro units.
        /// In theory, this should mean the conversion factor is 1f / 500, but
        /// for some reason, that makes everything feel too fast.
        /// Using 1f / 550 makes everything feel more accurate.
        /// Perhaps I measured Spyro's diameter wrong?  Who knows.
        /// </summary>
        private const float SpyroUnits = 1f / 550;
        private const float SpyroAnglesToDeg = 360f / 4096;
        private const float SpyroFrames = 1f / 30;
        private const float SpyroUnitsPerFrame = SpyroUnits / SpyroFrames;
        private const float SpyroUnitsPerFrameSquared = SpyroUnits / (SpyroFrames * SpyroFrames);
        private const float SpyroAnglesPerFrame = SpyroAnglesToDeg / SpyroFrames;

        public static class Default
        {
            public const float Gravity = 12 * SpyroUnitsPerFrameSquared;
            public const float JumpVSpeed = 110 * SpyroUnitsPerFrame;
            public const float JumpHoldGravity = -1.4f * SpyroUnitsPerFrameSquared;
            public const float MaxJumpHoldTime = 5 * SpyroFrames;
        }

        public static class Walk
        {
            public const float Speed = 143 * SpyroUnitsPerFrame;
            public const float Accel = 20 * SpyroUnitsPerFrameSquared;
            public const float Decel = 12 * SpyroUnitsPerFrameSquared;
            public const float RotSpeedDeg = 80 * SpyroAnglesPerFrame;

            public const float SlowPivotMinAngleDeg = 90;
            public const float SlowPivotTime = 0.5f;
        }

        public static class Charge
        {
            public const float InitialGroundSpeed = 83 * SpyroUnitsPerFrame;
            public const float MaxGroundSpeed = 245 * SpyroUnitsPerFrame;
            public const float GroundAccel = 122 * SpyroUnitsPerFrameSquared;
            public const float TurnSpeedDeg = 48 * SpyroAnglesPerFrame;

            public const float AirSpeed = 240 * SpyroUnitsPerFrame;

            public const float JumpVSpeed = Default.JumpVSpeed;

            public const float CameraDecayRate = 10;
            public const float CameraPitchDeg = 0;
            public const float CameraDistance = 5;
        }

        public static class Bonk
        {
            public const float Duration = 0.5f;
            public const float Distance = 1;
            public const float AngleDeg = 20;
        }

        public static class Glide
        {
            public const float TurnSpeedDeg = 90;
            public const float Gravity = 4 * SpyroUnitsPerFrameSquared;
            public const float TerminalVSpeed = -60 * SpyroUnitsPerFrame;

            public const float InitialFSpeed = 66 * SpyroUnitsPerFrame;
            public const float MaxFSpeed = 200 * SpyroUnitsPerFrame;
            public const float Accel = 8 * SpyroUnitsPerFrameSquared;

            public const float CameraDecayRate = 10;
            public const float CameraPitchDeg = 0;
            public const float CameraDistance = 7;
        }
    }
}