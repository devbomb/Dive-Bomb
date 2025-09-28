using System;
using Godot;

namespace FastDragon
{
    public partial class PlayerCameraFocusPoint : Node3D
    {
        [Export] public Player Player;
        [Export] public Node3D RestPos;

        public void Reset()
        {
            GlobalTransform = RestPos.GlobalTransform;
            this.ResetPhysicsInterpolation3D();
        }

        public override void _PhysicsProcess(double delta)
        {
            if (Player.CurrentState.UseMario64CameraFocus)
            {
                var groundPos = FindGroundPosition();
                float yFocusGround = groundPos.Y + RestPos.Position.Y;
                float targetYFocus = Mathf.Max(
                    yFocusGround,
                    Player.GlobalPosition.Y
                );

                var focusPos = RestPos.GlobalPosition;
                focusPos.Y = AccelMath.SmoothStepToward(
                    GlobalPosition.Y,
                    targetYFocus,
                    Player.Default.Gravity,
                    (float)delta,
                    ref _cameraFocusYSpeed
                );
                GlobalPosition = focusPos;
            }
            else
            {
                GlobalPosition = RestPos.GlobalPosition;
            }

            // HACK: Keep the camera focus rotated with the player, so recentering
            // works correctly.
            //
            // The camera uses the global rotation of whatever it's following to
            // determine where to go when it recenters.  Its follow target
            // happens to be the camera focus.  The camera focus is top-level
            // (I forget why), so it doesn't rotate when the player rotates.
            // Thus, it always has a global rotation of (0, 0, 0) unless we
            // intervene, so the camera always faces north when it recenters.
            // To avoid this, just manually change the focus's rotation.
            GlobalRotation = Player.GlobalRotation;
        }

        private float _cameraFocusYSpeed = 0;

        private Vector3 FindGroundPosition()
        {
            const float maxHeight = 10;
            var collision = Player.MoveAndCollide(
                Vector3.Down * maxHeight,
                testOnly: true,
                recoveryAsCollision: true
            );

            return collision == null
                ? (Player.GlobalPosition + (Vector3.Down * maxHeight))
                : collision.GetPosition();
        }
    }
}