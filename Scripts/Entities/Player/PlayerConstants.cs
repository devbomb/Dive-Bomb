using Godot;

namespace FastDragon
{
    public partial class Player
    {
        public static class Default
        {
            public const float Gravity = 30f;
            public const float JumpRiseGravity = Gravity * 0.5f;
            public const float JumpVSpeed = 10;
        }

        public static class Walk
        {
            public const float Speed = 5f;
            public const float Accel = 20;
            public const float RotSpeedDeg = 360;

            public const float SlowPivotMinAngleDeg = 90;
            public const float SlowPivotTime = 0.5f;
        }

        public static class Charge
        {
            public const float GroundSpeed = 10f;
            public const float GroundTurnSpeedDeg = 90;

            public const float AirSpeed = 9.5f;
            public const float AirTurnSpeedDeg = 135;

            public const float JumpVSpeed = 7;

            public const float CameraDecayRate = 10;
            public const float CameraPitchDeg = 0;
            public const float CameraDistance = 5;
        }

        public static class Bonk
        {
            public const float Duration = 0.5f;
            public const float Distance = 1;
            public const float AngleDeg = 10;
        }

        public static class Glide
        {
            public const float Speed = 8;
            public const float TurnSpeedDeg = 90;
            public const float Gravity = 2;

            public const float CameraDecayRate = 10;
            public const float CameraPitchDeg = 0;
            public const float CameraDistance = 7;
        }
    }
}