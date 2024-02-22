using System;
using System.Collections.Generic;
using Godot;

namespace FastDragon
{
    public partial class PlayerState : State
    {
        public virtual bool Invincible => false;
        public virtual bool DisableCameraInput => false;
        public virtual bool UseMario64CameraFocus => true;
        public virtual bool CanBoundAfterLanding => false;

        protected Player _player => _stateMachine.GetParent<Player>();

        /// <summary>
        /// Returns the direction (and magnitude) that the left stick is
        /// pointing in 3D space, rotated with respect to the camera.
        /// </summary>
        /// <returns></returns>
        protected Vector3 LeftStick3D()
        {
            Vector2 leftStick2D = InputService.LeftStick;
            Vector3 cameraRot = _player.Camera.Rotation;

            Vector3 unrotated =
                (Vector3.Right * leftStick2D.X) +
                (Vector3.Forward * leftStick2D.Y);

            return unrotated.Rotated(Vector3.Up, cameraRot.Y)
                            .LimitLength(1);
        }

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
            _player.FSpeed = forwardSpeed;
        }

        protected void RotateTowardLeftStick(float rotSpeedRad, float delta)
        {
            var leftStick2D = InputService.LeftStick;
            leftStick2D = leftStick2D.LimitLength(1);

            if (!leftStick2D.IsZeroApprox())
            {
                float targetYawRad = LeftStick3D().ForwardToEulerAnglesRad().Y;

                var rot = _player.GlobalRotation;
                rot.Y = AngleMath.MoveToward(rot.Y, targetYawRad, rotSpeedRad * delta);
                _player.GlobalRotation = rot;
            }
        }

        protected void RotateInstantlyTowardLeftStick()
        {
            var leftStick2D = InputService.LeftStick;
            leftStick2D = leftStick2D.LimitLength(1);

            if (!leftStick2D.IsZeroApprox())
            {
                float targetYawRad = LeftStick3D().ForwardToEulerAnglesRad().Y;

                var rot = _player.GlobalRotation;
                rot.Y = targetYawRad;
                _player.GlobalRotation = rot;
            }
        }

        protected void RotateInstantlyTowardVelocity()
        {
            Vector3 rot = _player.GlobalRotation;
            rot.Y = _player.Velocity
                .Flattened()
                .ForwardToEulerAnglesRad()
                .Y;
            _player.GlobalRotation = rot;
        }

        protected void RedirectFSpeedTowardYaw()
        {
            Vector3 vel = _player.GlobalForward() * _player.FSpeed;
            vel.Y = _player.VSpeed;
            _player.Velocity = vel;
        }

        protected void AccelerateWithLeftStickAgainstDrag(
            float maxSpeed,
            float minAccel,
            float maxAccel,
            float delta
        )
        {
            Vector3 leftStick3D = LeftStick3D();
            Vector3 flatVel = _player.Velocity.Flattened();

            // Apply a drag force in the opposite direction of the current
            // motion
            float flatSpeed = flatVel.Length();
            float drag = Mathf.Lerp(0, maxAccel, flatSpeed / maxSpeed);
            flatVel -= flatVel.Normalized() * drag * delta;

            // Apply acceleration in the direction the stick is being pushed.
            // If the stick isn't being pushed at all, then apply the minimum
            // acceleration in the current facing direction.
            float accel = Mathf.Lerp(minAccel, maxAccel, leftStick3D.Length());

            if (leftStick3D.IsZeroApprox())
            {
                flatVel += _player.GlobalForward() * accel * delta;
            }
            else
            {
                flatVel += leftStick3D.Normalized() * accel * delta;
            }

            // Save it
            flatVel.Y = _player.Velocity.Y;
            _player.Velocity = flatVel;
        }

        protected void AccelerateWithLeftStick(
            float maxSpeed,
            float maxAccel,
            float delta
        )
        {
            Vector3 leftStick3D = LeftStick3D();
            Vector3 flatVel = _player.Velocity.Flattened();

            // Apply a drag force in the opposite direction of the current
            // motion, but only if we're exceeding the speed limit
            float flatSpeed = flatVel.Length();
            if (flatSpeed > maxSpeed)
            {
                float drag = Mathf.Lerp(0, maxAccel, flatSpeed / maxSpeed);
                flatVel -= flatVel.Normalized() * drag * delta;
            }

            // Apply acceleration in the direction the stick is being pushed.
            float accel = leftStick3D.Length() * maxAccel;
            flatVel += leftStick3D.Normalized() * accel * delta;

            // Save it
            flatVel.Y = _player.Velocity.Y;
            _player.Velocity = flatVel;
        }

        protected void StrafeWithLeftStick(
            float maxSpeed,
            float accel,
            float delta
        )
        {
            Vector3 targetFlatVel = LeftStick3D() * maxSpeed;
            Vector3 flatVel = _player.Velocity.Flattened();
            flatVel = flatVel.MoveToward(targetFlatVel, accel * delta);

            _player.Velocity = new Vector3(
                flatVel.X,
                _player.Velocity.Y,
                flatVel.Z
            );
        }

        protected void ApplyGravity(
            float delta,
            float gravity = Player.Default.Gravity)
        {
            _player.Velocity += Vector3.Down * gravity * delta;
        }

        protected void DecelerateHSpeedToZero(float delta, float friction = Player.Walk.Decel)
        {
            var v = _player.Velocity.Flattened();
            v = v.MoveToward(Vector3.Zero, friction * delta);
            v.Y = _player.Velocity.Y;

            _player.Velocity = v;
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

        protected void AngleModelPitchWithGroundSlope(float delta)
        {
            Vector3 forwardOnFloor = _player
                .GlobalForward()
                .ProjectOnPlane(_player.GetFloorNormal());

            _player.Model.GlobalRotation = _player.Model.GlobalRotation.DecayTowardsEulerRad(
                forwardOnFloor.ForwardToEulerAnglesRad(),
                10,
                delta
            );
        }

        protected void AngleModelPitchWithVelocity(float delta)
        {
            var rot = _player.Velocity.Normalized().ForwardToEulerAnglesRad();
            _player.ModelPitchRad = rot.X;
        }

        protected void ResetModelPitch()
        {
            _player.ModelPitchRad = 0;
        }

        /// <summary>
        /// Just like MoveAndSlide, except:
        /// * It passes through all <see cref="IRollable"/> objects, unless
        ///     <see cref="IRollable.CausesBonk"/> is true.
        /// * It calls <see cref="IRollable.OnRolledInto"/> whenever on all
        ///     <see cref="IRollable"/> objects it touches or passes through.
        /// * It bonks the player if they touch an <see cref="IRollable"/>
        ///     whose <see cref="IRollable.CausesBonk"/> is true
        /// * It bonks the player if they hit a wall at too direct of an angle
        /// * It returns true if the player bonked
        /// </summary>
        /// <param name="delta"></param>
        protected bool MoveAndSlideRolling(float delta)
        {
            Vector3 prevPos = _player.GlobalPosition;
            Vector3 prevVel = _player.Velocity;

            _player.MoveAndSlide();

            int numCollisions = _player.GetSlideCollisionCount();
            for (int i = 0; i < numCollisions; i++)
            {
                var collision = _player.GetSlideCollision(i);

                // Trigger OnRolledInto().
                // Bonk if it's bonkable.
                var hitObject = collision.GetCollider();
                if (hitObject is IRollable c)
                {
                    c.OnRolledInto();

                    if (c.CausesBonk)
                        return Bonk();

                    // Rewind and try again, but this time ignore this object
                    _player.GlobalPosition = prevPos;
                    _player.Velocity = prevVel;

                    _player.AddCollisionExceptionWith((Node)hitObject);
                    bool bonked = MoveAndSlideRolling(delta);
                    _player.RemoveCollisionExceptionWith((Node)hitObject);

                    return bonked;
                }
            }

            // Bonk if moving into a wall at the bonk angle.
            // To determine the angle, we look at the difference between the old
            // velocity and the new velocity, instead of looking at the wall's
            // normal.
            //
            // Why?  Because this method works even when you're charging
            // straight into a corner.
            if (_player.IsOnWall())
            {
                // HACK: Sometimes, IsOnWall() will return true, even when there
                // is clearly no wall there.  This can result in the player
                // bonking on things they shouldn't, such as baskets.
                //
                // So, let's ask Godot which wall it thinks we're touching.
                // If it can't find a good answer, then we know it was
                // bullshitting us earlier.
                if (FindWall() == null)
                    return false;

                Vector3 prevVelFlat = prevVel.Flattened();
                Vector3 newVelFlat = _player.Velocity.Flattened();

                float speedPercent = newVelFlat.Length() / prevVelFlat.Length();
                float wallAngleRad = Mathf.DegToRad(90) - Mathf.Acos(speedPercent);
                float bonkAngleRad = Mathf.DegToRad(Player.Bonk.AngleDeg);

                if (wallAngleRad < bonkAngleRad)
                    return Bonk();
            }

            return false;

            bool Bonk()
            {
                _player.GlobalPosition = prevPos;
                _player.MoveAndCollide(prevVel * delta);
                _player.ChangeState<PlayerBonkState>();
                return true;
            }
        }

        /// <summary>
        /// Changes to the ledge-grabbing state and returns true, if there is
        /// a valid ledge to grab
        /// </summary>
        /// <returns></returns>
        protected bool TryGrabLedge()
        {
            if (_player.VSpeed >= 0)
                return false;

            if (!_player.IsOnWallOnly())
                return false;

            if (!_player.LedgeDetector.LedgeDetected)
                return false;

            float ledgeHeight = _player.LedgeDetector.LedgeHeight - _player.GlobalPosition.Y;
            float minHeight = _player.MinLedgeGrabHeight.GlobalPosition.Y - _player.GlobalPosition.Y;
            if (ledgeHeight > minHeight)
            {
                _player.ChangeState<PlayerLedgeGrabState>();
                return true;
            }

            return false;
        }

        private Node FindWall()
        {
            var wallNormal = _player.GetWallNormal();
            int numCollisions = _player.GetSlideCollisionCount();

            for (int i = 0; i < numCollisions; i++)
            {
                var collision = _player.GetSlideCollision(i);
                if (collision.GetNormal().IsEqualApprox(wallNormal))
                    return (Node)collision.GetCollider();
            }

            return null;
        }
    }
}