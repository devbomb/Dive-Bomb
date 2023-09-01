using System;
using Godot;

namespace FastDragon
{
    public static class InputService
    {
        public static Vector2 LeftStick => new Vector2(
            Input.GetAxis("LeftStickLeft", "LeftStickRight"),
            Input.GetAxis("LeftStickDown", "LeftStickUp")
        );

        public static Vector2 RightStick => new Vector2(
            Input.GetAxis("RightStickLeft", "RightStickRight"),
            Input.GetAxis("RightStickDown", "RightStickUp")
        );

        public static bool ChargeHeld => Input.IsActionPressed("Charge");

        public static bool JumpHeld => Input.IsActionPressed("Jump");

        public static bool JumpJustPressed(InputEvent ev)
            => ev.IsActionPressed("Jump");

        public static bool PauseJustPressed(InputEvent ev)
            => ev.IsActionPressed("Pause");

    }
}