using System;
using System.Collections.Generic;
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
            float rotSpeedRad,
            float delta
        )
        {
            var leftStick2D = InputService.LeftStick;
            leftStick2D = leftStick2D.LimitLength(1);

            if (leftStick2D.IsZeroApprox())
            {
                // Do not rotate; just move the horizontal velocity to zero.
                Vector3 vel = _player.Velocity.Flattened();
                vel = vel.MoveToward(
                    Vector3.Zero,
                    accel * delta
                );
                vel.Y = _player.Velocity.Y;

                _player.Velocity = vel;

                return;
            }

            // Rotate according to the target direction
            Vector3 cameraRot = _player.Camera.Rotation;

            Vector3 targetDir =
                (Vector3.Right * leftStick2D.X) +
                (Vector3.Forward * leftStick2D.Y);

            targetDir = targetDir.Rotated(Vector3.Up, cameraRot.Y);

            float targetYawRad = Transform3D.Identity
                    .LookingAt(targetDir, Vector3.Up)
                    .Basis
                    .GetEuler()
                    .Y;

            var rot = _player.GlobalRotation;
            rot.Y = AngleMath.MoveToward(rot.Y, targetYawRad, rotSpeedRad * delta);
            _player.GlobalRotation = rot;

            // Accelerate according to the magnitude, keeping the vertical
            // speed the same.
            float targetSpeed = leftStick2D.Length() * maxSpeed;
            float vspeed = _player.Velocity.Y;
            float hspeed = _player.Velocity.Flattened().Length();
            hspeed = Mathf.MoveToward(hspeed, targetSpeed, accel * delta);

            Vector3 newVel = _player.GlobalForward() * hspeed;
            newVel.Y = vspeed;
            _player.Velocity = newVel;
        }

        protected void ApplyGravity(
            float delta,
            float gravity = Player.Default.Gravity)
        {
            _player.Velocity += Vector3.Down * gravity * delta;
        }

        protected void SetVSpeed(float vspeed)
        {
            var vel = _player.Velocity;
            vel.Y = vspeed;
            _player.Velocity = vel;
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

        /// <summary>
        /// Just like MoveAndSlide, except it calls onCollision() every time it
        /// hits something.  The return value of onCollision() determines how
        /// the motion continues:
        /// * ContinueSliding will make it act exactly like MoveAndSlide()
        /// * ContinueThroughObject will make the player go right through this
        ///     object (and _only_ this object), as if it weren't there.  Use
        ///     this, for example, when the player charges through a breakable
        ///     object.
        /// * Stop will make it act exactly like MoveAndCollide().  No more
        ///     slides will be processed this frame.
        /// </summary>
        /// <param name="delta"></param>
        /// <param name="onCollision"></param>
        protected void MoveAndSlideStepByStep(
            float delta,
            Func<GodotObject, MoveAndSlideAction> onCollision
        )
        {
            Vector3 prevPos = _player.GlobalPosition;
            Vector3 prevVel = _player.Velocity;

            _player.MoveAndSlide();

            int numCollisions = _player.GetSlideCollisionCount();
            for (int i = 0; i < numCollisions; i++)
            {
                var collision = _player.GetSlideCollision(i);
                var action = onCollision(collision.GetCollider());

                switch (action)
                {
                    case MoveAndSlideAction.ContinueSliding: break;

                    case MoveAndSlideAction.ContinueThroughObject:
                    {
                        var objectToIgnore = (Node)collision.GetCollider();

                        // Rewind and try again, but this time ignore this
                        // object.
                        _player.GlobalPosition = prevPos;
                        _player.Velocity = prevVel;

                        _player.AddCollisionExceptionWith(objectToIgnore);
                        MoveAndSlideStepByStep(delta, onCollision);
                        _player.RemoveCollisionExceptionWith(objectToIgnore);

                        return;
                    }

                    case MoveAndSlideAction.Stop:
                    {
                        _player.GlobalPosition = prevPos;
                        _player.MoveAndCollide(prevVel * delta);
                        return;
                    }
                }
            }
        }
        protected delegate MoveAndSlideAction MoveAndSlideCollisionHandler(GodotObject collider);
        protected enum MoveAndSlideAction
        {
            ContinueSliding,
            ContinueThroughObject,
            Stop
        }

        protected MoveAndSlideAction OnChargedIntoSomething(GodotObject hitObject)
        {
            if (hitObject is IChargeable c)
            {
                c.OnCharged();
                return c.CausesBonk
                    ? MoveAndSlideAction.Stop
                    : MoveAndSlideAction.ContinueThroughObject;
            }

            return MoveAndSlideAction.ContinueSliding;
        }
    }
}