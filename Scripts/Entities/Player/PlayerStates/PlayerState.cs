using System;
using System.Collections.Generic;
using Godot;

namespace FastDragon
{
    public partial class PlayerState : Node
    {
        public virtual bool AllowFlaming => true;
        public virtual bool SpawningGemsHomeIn => false;

        protected Player _player => GetParent<Player>();

        public virtual void OnStateEntered() {}
        public virtual void OnStateExited() {}

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

            return unrotated.Rotated(Vector3.Up, cameraRot.Y);
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
                float targetYawRad = Transform3D.Identity
                        .LookingAt(LeftStick3D(), Vector3.Up)
                        .Basis
                        .GetEuler()
                        .Y;

                var rot = _player.GlobalRotation;
                rot.Y = AngleMath.MoveToward(rot.Y, targetYawRad, rotSpeedRad * delta);
                _player.GlobalRotation = rot;
            }
        }

        protected void AccelerateWithLeftStick(
            float maxSpeed,
            float accel,
            float decel,
            float delta
        )
        {
            var leftStick2D = InputService.LeftStick;
            leftStick2D = leftStick2D.LimitLength(1);
            float targetSpeed = leftStick2D.Length() * maxSpeed;

            float a = _player.FSpeed < targetSpeed
                ? accel
                : decel;

            _player.FSpeed = Mathf.MoveToward(
                _player.FSpeed,
                targetSpeed,
                a * delta
            );
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

        protected void GlideWithJumpButton(InputEvent ev)
        {
            if (InputService.JumpJustPressed(ev) && !_player.HasUsedGlide)
                _player.ChangeState<PlayerGlideState>();
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
            _player.Model.GlobalRotation = _player.Model.GlobalRotation.DecayTowardsEulerRad(
                _player.Velocity.Normalized().ForwardToEulerAnglesRad(),
                10,
                delta
            );
        }

        protected void ResetModelPitch()
        {
            var rot = _player.Model.Rotation;
            rot.X = 0;
            _player.Model.Rotation = rot;
        }

        /// <summary>
        /// Just like MoveAndSlide, except:
        /// * It passes through all <see cref="IChargeable"/> objects, unless
        ///     <see cref="IChargeable.CausesBonk"/> is true.
        /// * It calls <see cref="IChargeable.OnCharged"/> whenever on all
        ///     <see cref="IChargeable"/> objects it touches or passes through.
        /// * It bonks the player if they touch an <see cref="IChargeable"/>
        ///     whose <see cref="IChargeable.CausesBonk"/> is true
        /// * It bonks the player if they hit a wall at too direct of an angle
        /// * It returns true if the player bonked
        /// </summary>
        /// <param name="delta"></param>
        protected bool MoveAndSlideCharging(float delta)
        {
            Vector3 prevPos = _player.GlobalPosition;
            Vector3 prevVel = _player.Velocity;

            _player.MoveAndSlide();

            int numCollisions = _player.GetSlideCollisionCount();
            for (int i = 0; i < numCollisions; i++)
            {
                var collision = _player.GetSlideCollision(i);

                // Trigger OnCharged().
                // Bonk if it's bonkable.
                var hitObject = collision.GetCollider();
                if (hitObject is IChargeable c)
                {
                    c.OnCharged();

                    if (c.CausesBonk)
                        return Bonk();

                    // Rewind and try again, but this time ignore this object
                    _player.GlobalPosition = prevPos;
                    _player.Velocity = prevVel;

                    _player.AddCollisionExceptionWith((Node)hitObject);
                    bool bonked = MoveAndSlideCharging(delta);
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