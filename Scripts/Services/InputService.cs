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

        public static bool PauseJustPressed(InputEvent ev)
            => ev.IsActionPressed("Pause");
    }
}