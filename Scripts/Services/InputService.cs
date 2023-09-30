using System;
using Godot;

namespace FastDragon
{
    public static class InputService
    {
        public const float StickDeadzone = 0.1f;

        public static Vector2 LeftStick => LeftStickRaw.Length() > StickDeadzone
            ? LeftStickRaw
            : Vector2.Zero;

        public static Vector2 RightStick => RightStickRaw.Length() > StickDeadzone
            ? RightStickRaw
            : Vector2.Zero;

        public static Vector2 LeftStickRaw => new Vector2(
            GetAxisRaw("LeftStickLeft", "LeftStickRight"),
            GetAxisRaw("LeftStickDown", "LeftStickUp")
        );

        public static Vector2 RightStickRaw => new Vector2(
            GetAxisRaw("RightStickLeft", "RightStickRight"),
            GetAxisRaw("RightStickDown", "RightStickUp")
        );

        private static float GetAxisRaw(
            StringName negativeAction,
            StringName positiveAction
        )
        {
            float pos = Input.GetActionRawStrength(positiveAction);
            float neg = Input.GetActionRawStrength(negativeAction);
            return pos - neg;
        }

        public static bool ChargeHeld => Input.IsActionPressed("Charge");

        public static bool JumpHeld => Input.IsActionPressed("Jump");

        public static bool JumpJustPressed(InputEvent ev)
            => ev.IsActionPressed("Jump");

        public static bool PauseJustPressed(InputEvent ev)
            => ev.IsActionPressed("Pause");

        public static bool FlameJustPressed(InputEvent ev)
            => ev.IsActionPressed("Flame");

    }
}