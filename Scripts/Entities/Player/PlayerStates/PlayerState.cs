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
            float delta
        )
        {
            var leftStick2D = InputService.LeftStick;
            leftStick2D = leftStick2D.LimitLength(1);
            float targetSpeed = leftStick2D.Length() * maxSpeed;

            _player.FSpeed = Mathf.MoveToward(
                _player.FSpeed,
                targetSpeed,
                accel * delta
            );
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

        protected bool IsTouchingWallAtBonkAngle()
        {
            if (!_player.IsOnWall())
                return false;

            var fwd = _player.GlobalForward();
            float wallAngleRad = GetCombinedWallNormals().Flattened().AngleTo(-fwd);
            float wallAngleDeg = Mathf.RadToDeg(wallAngleRad);

            return wallAngleDeg < Player.Bonk.AngleDeg;

            Vector3 GetCombinedWallNormals()
            {
                // If the player is charging straight into a corner,
                // GetWallNormal() will only return one of the wall's normals,
                // which will make it look like the player is grazing one wall
                // (when in reality, they're hitting two walls head-on).
                //
                // The solution: take the average normal of all walls we're
                // touching.
                var total = Vector3.Zero;

                int collisions = _player.GetSlideCollisionCount();
                for (int i = 0; i < collisions; i++)
                {
                    var collision = _player.GetSlideCollision(i);
                    var normal = collision.GetNormal();

                    float angleFromGroundRad = normal.AngleTo(Vector3.Up);
                    bool isWall = angleFromGroundRad > Mathf.DegToRad(_player.FloorMaxAngle);

                    if (isWall)
                    {
                        total += normal;
                    }
                }

                return total.Normalized();
            }
        }
    }
}