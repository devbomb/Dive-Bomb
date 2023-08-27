using Godot;
using System;

namespace FastDragon
{
    public partial class PlayerWalkState : PlayerState
    {
        public override void _PhysicsProcess(double deltaD)
        {
            var leftStick2D = InputService.LeftStick;
            Vector3 leftStick3D =
                (Vector3.Right * leftStick2D.X) +
                (Vector3.Forward * leftStick2D.Y);
            
            Vector3 cameraRot = GetViewport().GetCamera3D().Rotation;
            leftStick3D = leftStick3D.Rotated(Vector3.Up, cameraRot.Y);

            _player.Velocity = leftStick3D;
            _player.MoveAndSlide();
        }
    }
}

