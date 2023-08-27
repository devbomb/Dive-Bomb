using Godot;
using System;

namespace FastDragon
{
    public partial class PlayerWalkState : PlayerState
    {
        public override void _PhysicsProcess(double deltaD)
        {
            var leftStick = InputService.LeftStick;

            _player.Velocity =
                (Vector3.Right * leftStick.X) +
                (Vector3.Forward * leftStick.Y);
            _player.MoveAndSlide();
        }
    }
}

