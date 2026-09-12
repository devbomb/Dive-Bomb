using System;
using System.Collections.Generic;
using System.Linq;

using Godot;

namespace FastDragon
{
    public partial class PlayerState : State<Player>
    {
        public virtual bool Invincible => false;
        public virtual bool DisableCameraInput => false;
        public virtual bool UseMario64CameraFocus => true;
        public virtual bool CanBoundAfterLanding => false;

        public virtual bool PauseDamageCooldownTimer => false;

        /// <summary>
        /// Returns the direction (and magnitude) that the left stick is
        /// pointing in 3D space, rotated with respect to the camera.
        /// </summary>
        /// <returns></returns>
        protected Vector3 LeftStick3D()
        {
            Vector2 leftStick2D = InputService.LeftStick;
            Vector3 cameraRot = GetViewport().GetCamera3D().GlobalRotation;

            Vector3 unrotated =
                (Vector3.Right * leftStick2D.X) +
                (Vector3.Forward * leftStick2D.Y);

            return unrotated.Rotated(Vector3.Up, cameraRot.Y)
                            .LimitLength(1);
        }

        protected void RotateTowardLeftStick(float rotSpeedRad, float delta)
        {
            var leftStick2D = InputService.LeftStick;
            leftStick2D = leftStick2D.LimitLength(1);

            if (!leftStick2D.IsZeroApprox())
            {
                float targetYawRad = LeftStick3D().ForwardToEulerAnglesRad().Y;

                var rot = Self.GlobalRotation;
                rot.Y = AngleMath.MoveToward(rot.Y, targetYawRad, rotSpeedRad * delta);
                Self.GlobalRotation = rot;
            }
        }

        protected void RotateInstantlyTowardLeftStick()
        {
            var leftStick2D = InputService.LeftStick;
            leftStick2D = leftStick2D.LimitLength(1);

            if (!leftStick2D.IsZeroApprox())
            {
                float targetYawRad = LeftStick3D().ForwardToEulerAnglesRad().Y;

                var rot = Self.GlobalRotation;
                rot.Y = targetYawRad;
                Self.GlobalRotation = rot;
            }
        }

        protected void RotateInstantlyTowardVelocity()
        {
            if (Self.LocalVelocity.Flattened().IsZeroApprox())
                return;

            Vector3 rot = Self.GlobalRotation;
            rot.Y = Self.LocalVelocity
                .Flattened()
                .ForwardToEulerAnglesRad()
                .Y;
            Self.GlobalRotation = rot;
        }

        protected void AccelerateWithLeftStick(
            float maxSpeed,
            float maxAccel,
            float delta
        )
        {
            Vector3 leftStick3D = LeftStick3D();
            Vector3 flatVel = Self.LocalVelocity.Flattened();

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
            flatVel.Y = Self.LocalVelocity.Y;
            Self.LocalVelocity = flatVel;
        }

        protected void StrafeWithLeftStick(
            float maxSpeed,
            float accel,
            float delta
        )
        {
            Vector3 targetFlatVel = LeftStick3D() * maxSpeed;
            Vector3 flatVel = Self.LocalVelocity.Flattened();
            flatVel = flatVel.MoveToward(targetFlatVel, accel * delta);

            Self.LocalVelocity = new Vector3(
                flatVel.X,
                Self.LocalVelocity.Y,
                flatVel.Z
            );
        }

        protected void ApplyGravity(
            float delta,
            float gravity = Player.Default.Gravity)
        {
            Self.LocalVelocity += Vector3.Down * gravity * delta;
        }

        protected void ResetModelPitch()
        {
            Self.ModelPitchRad = 0;
        }

        /// <summary>
        ///     Returns true if the given collision happened at an angle that
        ///     would cause the player to bonk.
        /// </summary>
        protected bool IsBonkAngle(KinematicCollision3D collision)
        {
            // Floor collisions can never cause a bonk.
            if (collision.GetAngle() <= Self.FloorMaxAngle)
                return false;

            float angleToNormalRad = collision
                    .GetNormal()
                    .AngleTo(Self.GlobalForward());

            float angleToWallRad = Mathf.DegToRad(180) - angleToNormalRad;
            return angleToWallRad < Player.Bonk.AngleRad;
        }

        /// <summary>
        /// Scans the given area for damageable objects and damages them if
        /// they're vulnerable to this particular kind of attack.
        ///
        /// When an object is damaged in this manner, its <see cref="IDamageable.OnDamaged"/>
        /// method is called and a screen shake effect is played.
        ///
        /// If a damageable object is detected but it isn't vulnerable to this
        /// particular kind of attack, its <see cref="IDamageable.OnDamageRejected"/>
        /// method is called.
        ///
        /// If a damageable object is detected but it also appears inside
        /// <paramref name="objectsToIgnore"/>, then NEITHER <see cref="IDamageable.OnDamaged"/>
        /// NOR <see cref="IDamageable.OnDamageRejected"/> will be called.
        /// </summary>
        /// <param name="hitbox"></param>
        /// <param name="objectsToIgnore"></param>
        /// <param name="tryDamage">
        ///     Called when a damageable object is detected, regardless of if
        ///     it's vulnerable or not.  Will not be called if the object
        ///     appears inside <paramref name="objectsToIgnore"/>.
        /// </param>
        protected void ApplyHitboxToDamageableObjects(
            Area3D hitbox,
            List<IDamageable> objectsToIgnore,
            Func<IDamageable, bool> tryDamage
        )
        {
            var bodies = hitbox.GetOverlappingBodies();
            var areas = hitbox.GetOverlappingAreas();

            foreach (var body in bodies)
            {
                if (body is IDamageable d)
                    TryDamage(d);
            }

            foreach (var area in areas)
            {
                if (area is IDamageable d)
                    TryDamage(d);
            }

            void TryDamage(IDamageable d)
            {
                if (objectsToIgnore?.Contains(d) ?? false)
                {
                    GD.Print($"Ignoring already-damaged object: {d}");
                    return;
                }

                if (tryDamage(d))
                {
                    Self.Camera.Shake(
                        d.CameraShakeMagnitude,
                        d.CameraShakeFrequency,
                        d.CameraShakeDuration
                    );
                }
            }
        }

        /// <summary>
        /// Changes to the ledge-grabbing state and returns true, if there is
        /// a valid ledge to grab and the position we'd be hanging from is
        /// unblocked.
        /// </summary>
        /// <returns></returns>
        protected bool TryGrabLedge()
        {
            var ledge = Self.LedgeDetector.DetectLedge();

            if (!ledge.HasValue)
                return false;

            if (!Self.IsOnWallOnly())
                return false;

            if (ledge.Value.IsClimbingPathBlocked)
            {
                GD.Print("Ledge detected and on a wall, but the climbing path is blocked");
                return false;
            }

            if (ledge.Value.IsHangingPosBlocked)
            {
                GD.Print("Ledge detected and on a wall, but hanging pos is blocked");
                return false;
            }

            ChangeState<PlayerLedgeGrabState>();
            return true;
        }

        /// <summary>
        /// Changes to <see cref="PlayerWalkState"/> or <see cref="PlayerStandState"/>,
        /// depending on if the player is below the min walking speed or not.
        ///
        /// This is primarily for landing from one of the various jumping states
        /// </summary>
        protected void StartWalkingOrStanding()
        {
            if (Self.LocalVelocity.Length() < Player.Walk.MinSpeed)
                Self.ChangeState<PlayerStandState>();
            else
                Self.ChangeState<PlayerWalkState>();
        }
    }
}