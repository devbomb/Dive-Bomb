using Godot;

namespace FastDragon
{
    public partial class Player
    {
        public static class Default
        {
            public const float Gravity = 9.8f;
            public const float JumpRiseGravity = Gravity * 0.5f;
            public const float JumpVSpeed = 10;
        }

        public static class Walk
        {
            public const float Speed = 5f;
            public const float Accel = 20;
        }

        public static class Charge
        {
            public const float Speed = 10f;
            public const float TurnSpeedDeg = 90;

            public const float CameraDecayRate = 10;
            public const float CameraPitchDeg = 0;
            public const float CameraDistance = 5;
        }

        public static class Glide
        {
            public const float Speed = 10f;
            public const float TurnSpeedDeg = 90;
            public const float Gravity = 2;

            public const float CameraDecayRate = 10;
            public const float CameraPitchDeg = 0;
            public const float CameraDistance = 5;
        }
    }
}