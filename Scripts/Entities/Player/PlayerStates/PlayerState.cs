using Godot;

namespace FastDragon
{
    public partial class PlayerState : Node
    {
        protected Player _player => GetParent<Player>();
        protected Node3D _model => GetNode<Node3D>("%Model");

        public virtual void OnStateEntered() {}
        public virtual void OnStateExited() {}

        /// <summary>
        /// Propels the player forward, letting them steer using the left
        /// stick's x axis.
        ///
        /// This changes the player's velocity, but does not call MoveAndSlide()
        /// or MoveAndCollide().
        ///
        /// Only the player's x and z velocities are affected; the y velocity
        /// remains untouched.
        ///
        /// Use this for states where the player automatically moves forward,
        /// such as charging or gliding.
        /// </summary>
        protected void TurningControls(
            float forwardSpeed,
            float turnSpeedDeg,
            float delta
        )
        {
            // Rotate with the left stick
            float rotDeg = _player.RotationDegrees.Y;
            rotDeg -= InputService.LeftStick.X * turnSpeedDeg * delta;
            _player.RotationDegrees = new Vector3(0, rotDeg, 0);

            // Update the horizontal velocity, without changing the vertical
            // speed.
            Vector3 newVel = _player.GlobalForward() * forwardSpeed;
            newVel.Y = _player.Velocity.Y;
            _player.Velocity = newVel;
        }

        /// <summary>
        /// Accelerates the player in the direction the left stick is pointing,
        /// up to the given max speed.
        ///
        /// Only the player's x and z velocities are affected; the y velocity
        /// is left untouched.
        ///
        /// The left stick input is rotated relative to the camera.
        ///
        /// The player's Y-rotation will be updated to match their new x and z
        /// velocities.
        ///
        /// Use this for states where the player has more precise control over
        /// their character, such as when walking, or strafing in mid-air.
        /// </summary>
        /// <param name="maxSpeed"></param>
        /// <param name="accel"></param>
        /// <param name="delta"></param>
        protected void WalkControls(
            float maxSpeed,
            float accel,
            float delta
        )
        {
            var leftStick2D = InputService.LeftStick;
            leftStick2D = leftStick2D.LimitLength(1);

            Vector3 cameraRot = _player.Camera.Rotation;
            Vector3 leftStick3D =
                (Vector3.Right * leftStick2D.X) +
                (Vector3.Forward * leftStick2D.Y);
            leftStick3D = leftStick3D.Rotated(Vector3.Up, cameraRot.Y);

            // Update the velocity without affecting the vertical speed.
            Vector3 vel = _player.Velocity.Flattened();
            vel = _player.Velocity.MoveToward(
                leftStick3D * maxSpeed,
                accel * delta
            );
            vel.Y = _player.Velocity.Y;

            _player.Velocity = vel;

            // Update the rotation
            if (!_player.Velocity.Flattened().IsZeroApprox())
            {
               float yAngleRad = Transform3D.Identity
                    .LookingAt(_player.Velocity.Flattened(), Vector3.Up)
                    .Basis
                    .GetEuler()
                    .Y;

                var rot = _player.GlobalRotation;
                rot.Y = yAngleRad;
                _player.GlobalRotation = rot;
            }
        }

        protected void ApplyGravity(
            float delta,
            float gravity = Player.Default.Gravity)
        {
            _player.Velocity += Vector3.Down * gravity * delta;
        }

        /// <summary>
        /// Gradually moves the camera behind the player, using exponential
        /// decay to smooth things out.
        /// </summary>
        protected void ContinuouslyRecenterCamera(
            float cameraDistance,
            float cameraPitchDeg,
            float decayRate,
            float delta
        )
        {
            var camera = _player.Camera;

            camera.OrbitDistance = MathUtils.DecayToward(
                camera.OrbitDistance,
                cameraDistance,
                decayRate,
                delta
            );

            camera.OrbitPitchRad = AngleMath.DecayToward(
                camera.OrbitPitchRad,
                Mathf.DegToRad(cameraPitchDeg),
                decayRate,
                delta
            );

            camera.OrbitYawRad = AngleMath.DecayToward(
                camera.OrbitYawRad,
                _player.GlobalRotation.Y,
                decayRate,
                delta
            );
        }
    }
}