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
        }

        public static class Stand
        {
            // The real game uses a value of 180, but I'd like to make _this_
            // game feel a little bit more responsive than that.
            public const float RotSpeedDeg = 270 * SpyroAnglesPerFrame;
        }

        public static class Walk
        {
            public const float MinSpeed = 15 * SpyroUnitsPerFrame;
            public const float Speed = 143 * SpyroUnitsPerFrame;
            public const float Accel = 20 * SpyroUnitsPerFrameSquared;
            public const float Decel = 12 * SpyroUnitsPerFrameSquared;
            public const float RotSpeedDeg = 80 * SpyroAnglesPerFrame;
        }

        public static class SlowPivot
        {
            public const float RotSpeedDeg = 196 * SpyroAnglesPerFrame;
            public const float MaxSkidDuration = 11 * SpyroFrames;
            public const float MinDecel = 13 * SpyroUnitsPerFrameSquared; // Could also be 12, depending on rounding

            public const float MinAngleDeg = 135;
        }

        public static class Jump
        {
            // In Spyro, the walk rot speed is the same as the jump rot speed.
            // In theory, they should be the same in this game, too.
            // In practice, keeping them the same somehow feels _worse_ in this
            // game than in it does in Spyro.  So, let's just give it a little
            // boost.  #NotSorry.
            public const float RotSpeedDeg = Walk.RotSpeedDeg * 1.25f;

            public const float MaxFSpeed = 100 * SpyroUnitsPerFrame;
            public const float StrafeAccel = 20 * SpyroUnitsPerFrameSquared;


            public const float FullJumpHeight = 1300 * SpyroUnits;
            public const float FullJumpRiseTime = 15 * SpyroFrames;
            public static readonly float FullJumpRiseGravity;

            public const float MinJumpHeight = 530 * SpyroUnits;
            public const float MinJumpRiseTime = 9 * SpyroFrames;
            public static readonly float ShortHopGravity;

            public static readonly float InitVSpeed;

            static Jump()
            {
                (InitVSpeed, FullJumpRiseGravity) = AccelMath.SpeedAndFrictionNeededForDistanceAndTime(
                    FullJumpHeight,
                    FullJumpRiseTime
                );

                ShortHopGravity = AccelMath.FrictionNeededForDistance(MinJumpHeight, InitVSpeed);
            }
        }

        public static class Charge
        {
            public const float InitialGroundSpeed = 83 * SpyroUnitsPerFrame;
            public const float MaxGroundSpeed = 245 * SpyroUnitsPerFrame;
            public const float GroundAccel = 122 * SpyroUnitsPerFrameSquared;
            public const float TurnSpeedDeg = 48 * SpyroAnglesPerFrame;

            public const float AirSpeed = 240 * SpyroUnitsPerFrame;

            public static float JumpVSpeed = 110 * SpyroUnitsPerFrame;

            public const float CameraDecayRate = 10;
            public const float CameraPitchDeg = 0;
            public const float CameraDistance = 5;
        }

        public static class Bonk
        {
            public const float Duration = 0.5f;
            public const float Distance = 1;
            public const float AngleDeg = 45;
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