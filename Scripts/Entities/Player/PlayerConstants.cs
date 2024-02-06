using Godot;

namespace FastDragon
{
    public partial class Player
    {
        public static class Default
        {
            public const float Gravity = 20;
        }

        public static class Stand
        {
            public const float RotSpeedDeg = 712f;
        }

        public static class Walk
        {
            public const float MinSpeed = 0.8f;
            public const float Speed = 7.8f;
            public const float Accel = 32.7f;
            public const float Decel = 20;
            public const float RotSpeedDeg = 211f;
        }

        public static class Kick
        {
            public const float Duration = 0.5f;
            public const float JumpHeight = 0.5f;

            public static readonly float InitVSpeed = AccelMath.SpeedNeededForDistance(
                JumpHeight,
                Default.Gravity
            );
        }

        public static class Roll
        {
            public const float InitialSpeed = 15;
            public const float MinSpeed = 5;
            public const float MinAccel = 20;
            public const float MaxAccel = 30;

            public const float Duration = 0.5f;
            public const float FrictionlessDuration = 0.25f;
            public const float Friction = (InitialSpeed - MinSpeed) / (Duration - FrictionlessDuration);
        }

        public static class Jump
        {
            // In Spyro, the walk rot speed is the same as the jump rot speed.
            // In theory, they should be the same in this game, too.
            // In practice, keeping them the same somehow feels _worse_ in this
            // game than in it does in Spyro.  So, let's just give it a little
            // boost.  #NotSorry.
            public const float RotSpeedDeg = 720;

            public const float MaxFSpeed = 5.5f;
            public const float StrafeAccel = 20;


            public const float FullJumpHeight = 2.4f;
            public const float FullJumpRiseTime = 0.5f;
            public static readonly float FullJumpRiseGravity;

            public const float MinJumpHeight = 1;
            public const float MinJumpRiseTime = 0.3f;
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

        public static class Dive
        {
            public const float TurnSpeedDeg = 126.6f;

            public const float FSpeed = 12;
            public const float InitialVSpeed = 12;
            public const float Gravity = 40;

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

        public static class LedgeGrab
        {
            public const float ClimbDuration = 0.5f;
            public const float ClimbForwardDist = 0.5f;
        }

        public static class Glide
        {
            public const float MaxFSpeed = 11f;
            public const float CameraDistance = 7;
        }
    }
}