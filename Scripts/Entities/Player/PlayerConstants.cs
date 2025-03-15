using Godot;

namespace FastDragon
{
    public partial class Player
    {
        public const int MaxHealth = 4;

        public static class Default
        {
            public const float Gravity = 20;
            public const float CoyoteTime = 5f / 60;
            public const float EarlyJumpBufferTime = 5f / 60;
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
            public static float RotSpeedRad => Mathf.DegToRad(RotSpeedDeg);

            public const float MinTimeBeforeSideFlip = 0.25f;

            public const float MaxRunTiltDeg = 22.5f;
            public static float MaxRunTiltRad => Mathf.DegToRad(MaxRunTiltDeg);
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

            public const float RedirectTimeWindow = 0.1f;

            public const float Duration = 0.5f;
            public const float FrictionlessDuration = 0.25f;
            public const float Friction = (InitialSpeed - MinSpeed) / (Duration - FrictionlessDuration);
        }

        public static class Jump
        {
            public const float RotSpeedDeg = 720;
            public static float RotSpeedRad => Mathf.DegToRad(RotSpeedDeg);

            public const float MaxFSpeed = Walk.Speed;
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

        public static class BoundJump
        {
            public const float TimeWindow = 0.25f;

            public const float FullJumpHeight = 4;
            public const float FullJumpRiseTime = 0.5f;
            public static readonly float FullJumpRiseGravity;

            public const float MinJumpHeight = 1.5f;
            public const float MinJumpRiseTime = 0.3f;
            public static readonly float ShortHopGravity;

            public static readonly float InitVSpeed;

            static BoundJump()
            {
                (InitVSpeed, FullJumpRiseGravity) = AccelMath.SpeedAndFrictionNeededForDistanceAndTime(
                    FullJumpHeight,
                    FullJumpRiseTime
                );

                ShortHopGravity = AccelMath.FrictionNeededForDistance(MinJumpHeight, InitVSpeed);
            }
        }

        public static class SideFlip
        {
            public const float TimeWindow = 0.4f;

            public const float FullJumpHeight = 4;
            public const float FullJumpRiseTime = 0.5f;
            public static readonly float FullJumpRiseGravity;

            public const float MinJumpHeight = 3f;
            public const float MinJumpRiseTime = 0.6f;
            public static readonly float ShortHopGravity;

            public static readonly float InitVSpeed;

            static SideFlip()
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
            public const float TurnSpeedDeg = 150f;
            public static float TurnSpeedRad => Mathf.DegToRad(TurnSpeedDeg);

            public const float FSpeed = 12;
            public const float InitialVSpeed = 12;
            public const float Gravity = 40;

            public const float CameraDecayRate = 10;
            public const float CameraPitchDeg = 0;
            public static float CameraPitchRad => Mathf.DegToRad(CameraPitchDeg);
            public const float CameraDistance = 5;

            public const float RedirectTimeWindow = 0.1f;
        }

        public static class Bonk
        {
            public const float RecoilDuration = 0.5f;
            public const float RecoilDistance = 1;
            public const float RecoilHeight = 1;
            public const float AngleDeg = 45;
            public static float AngleRad => Mathf.DegToRad(AngleDeg);

            public const float RecoverDuration = 1;

            public static readonly float Friction;
            public static readonly float InitHSpeed;

            public static readonly float Gravity;
            public static readonly float InitVSpeed;

            static Bonk()
            {
                (InitHSpeed, Friction) = AccelMath.SpeedAndFrictionNeededForDistanceAndTime(
                    RecoilDistance,
                    RecoilDuration
                );


                (InitVSpeed, Gravity) = AccelMath.SpeedAndFrictionNeededForDistanceAndTime(
                    RecoilHeight,
                    RecoilDuration / 2
                );
            }
        }

        public static class LedgeGrab
        {
            public const float ClimbDuration = 0.5f;
            public const float ClimbForwardDist = 0.5f;
        }

        public static class WallJump
        {
            public const float FSpeed = Walk.Speed;
            public const float DisableStrafeDuration = 0.15f;
        }

        public static class Glide
        {
            public const float MaxFSpeed = 11f;
            public const float CameraDistance = 7;
        }
    }
}