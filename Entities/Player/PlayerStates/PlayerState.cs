using System;
using System.Collections.Generic;
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
        /// Just like MoveAndSlide, except:
        /// * It passes through all objects that meet the isVulnerable() criteria
        /// * It puts all passed-through objects into the brokenObjects list
        /// * It puts objects that failed the isVulnerable() criteria into the
        ///     unbrokenObjects list
        /// * It bonks the player if they touch an object that meets both the
        ///     isVulnerable() and causesBonkWhenBroken() criteria
        /// * It bonks the player if they hit a wall at too direct of an angle
        /// * It returns true if the player bonked
        ///
        /// It puts the broken/unbroken objects in the provided list instead of
        //  returning them in an array to avoid allocating on the heap every
        /// frame.  Remember to clear those lists you pass in before calling!
        /// </summary>
        /// <param name="delta"></param>
        protected bool MoveAndSlideBreakingObjects(
            Func<IBreakable, bool> isVulnerable,
            List<IBreakable> brokenObjects,
            List<IBreakable> unbrokenObjects,
            float delta
        )
        {
            Vector3 prevPos = Self.GlobalPosition;
            Vector3 prevVel = Self.Velocity;

            Self.MoveAndSlide();

            int numCollisions = Self.GetSlideCollisionCount();
            for (int i = 0; i < numCollisions; i++)
            {
                var collision = Self.GetSlideCollision(i);
                var hitObject = collision.GetCollider();

                if (hitObject is not IBreakable b)
                    continue;

                if (isVulnerable(b))
                {
                    brokenObjects.Add(b);

                    if (b.CausesBonk)
                        return Bonk();

                    // Rewind and try again, but this time ignore this object
                    Self.GlobalPosition = prevPos;
                    Self.Velocity = prevVel;

                    Self.AddCollisionExceptionWith((Node)hitObject);
                    bool bonked = MoveAndSlideBreakingObjects(
                        isVulnerable,
                        brokenObjects,
                        unbrokenObjects,
                        delta);
                    Self.RemoveCollisionExceptionWith((Node)hitObject);

                    return bonked;
                }
                else
                {
                    unbrokenObjects.Add(b);
                }
            }

            // Bonk if moving into a wall at the bonk angle.
            // We detect this by measuring the player's change in speed, rather
            // than by calling IsOnWall() or reading the wall normal.
            // Why?  Well:
            // 1. IsOnWall() sometimes gives us a false positive, potentially
            //      causing a bonk against things that shouldn't be bonkable
            //      (EG: baskets).
            //
            // 2. Reading the wall normal doesn't work if they player is touching
            //      two walls at once(IE: rolling into a corner).  Even if those
            //      two walls "add up" to being a head-on collision, Godot will
            //      only use ONE of those walls' normals, which would result in
            //      the player not bonking when they logically should.
            //
            // 3. Let's face it: why do people feel pain when they slam into a
            //      wall IRL?  It's not the collision itself, but rather the
            //      _deceleration_ caused by the collision.  Therefore, it makes
            //      sense for a bonk to be triggered by a sudden stop.
            Vector3 prevVelFlat = prevVel.Flattened();
            Vector3 newVelFlat = Self.Velocity.Flattened();

            float speedPercent = newVelFlat.Length() / prevVelFlat.Length();
            float wallAngleRad = Mathf.DegToRad(90) - Mathf.Acos(speedPercent);
            float bonkAngleRad = Mathf.DegToRad(Player.Bonk.AngleDeg);

            if (wallAngleRad < bonkAngleRad)
            {
                // HACK: Don't bonk if the contact point is too low.
                // TODO: Explain why.
                float highestContactPoint = float.MinValue;
                for (int i = 0; i < numCollisions; i++)
                {
                    float height = Self.GetSlideCollision(i).GetPosition().Y;
                    if (height > highestContactPoint)
                        highestContactPoint = height;
                }

                float heightAbovePlayer = highestContactPoint - Self.GlobalPosition.Y;
                const float forgivableHeight = 0.5f;
                if (heightAbovePlayer < forgivableHeight)
                {
                    GD.Print($"Attempting bonk forgiveness (contact height: {heightAbovePlayer})");
                    Vector3 posBeforeForgivenessAttempt = Self.GlobalPosition;
                    Vector3 velBeforeForgivenessAttempt = Self.Velocity;

                    Self.GlobalPosition = prevPos + (Vector3.Up * heightAbovePlayer);
                    Self.Velocity = prevVel;
                    bool forgivenessFailed = Self.MoveAndSlide();

                    if (forgivenessFailed)
                    {
                        GD.Print("Bonk forgiveness failed.  Should've repented!");
                        Self.GlobalPosition = posBeforeForgivenessAttempt;
                        Self.Velocity = velBeforeForgivenessAttempt;
                        return Bonk();
                    }

                    GD.Print("Bonk forgiven.");
                    return false;
                }

                return Bonk();
            }

            return false;

            bool Bonk()
            {
                Self.GlobalPosition = prevPos;
                Self.MoveAndCollide(prevVel * delta);
                Self.ChangeState<PlayerBonkState>();

                for (int i = 0; i < numCollisions; i++)
                {
                    var collision = Self.GetSlideCollision(i);
                    if (collision.GetCollider() is IBonkable b)
                    {
                        b.OnBonked();
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Scans the given area for breakable objects and breaks them if they're
        /// vulnerable to this particular kind of attack.
        ///
        /// When an object is broken in this manner, its <see cref="IBreakable.OnBroken"/>
        /// method is called, the <paramref name="onBroken"/> callback is fired,
        /// and a screen shake effect is played.
        ///
        /// If a breakable object is detected but it isn't vulnerable to this
        /// particular kind of attack, its <see cref="IBreakable.OnBreakRejected"/>
        /// method is called.
        ///
        /// If a breakable object is detected but it also appears inside
        /// <paramref name="objectsToIgnore"/>, then NEITHER <see cref="IBreakable.OnBroken"/>
        /// NOR <see cref="IBreakable.OnBreakRejected"/> will be called.
        /// </summary>
        /// <param name="hitbox"></param>
        /// <param name="objectsToIgnore"></param>
        /// <param name="isVulnerable"></param>
        /// <param name="onDetected">
        ///     Called when a breakable object is detected, regardless of if
        ///     it's vulnerable or not.  It will also not be called if the
        ///     object appears inside <paramref name="objectsToIgnore"/>.
        /// </param>
        protected void ApplyHitboxToBreakableObjects(
            Area3D hitbox,
            List<IBreakable> objectsToIgnore,
            Func<IBreakable, bool> isVulnerable,
            Action<IBreakable> onDetected)
        {
            var bodies = hitbox.GetOverlappingBodies();
            var areas = hitbox.GetOverlappingAreas();

            foreach (var body in bodies)
            {
                if (body is IBreakable b)
                    TryBreak(b);
            }

            foreach (var area in areas)
            {
                if (area is IBreakable b)
                    TryBreak(b);
            }

            void TryBreak(IBreakable b)
            {
                if (objectsToIgnore?.Contains(b) ?? false)
                {
                    GD.Print($"Ignoring already-broken object: {b}");
                    return;
                }

                onDetected(b);

                if (!isVulnerable(b))
                {
                    b.OnBreakRejected();
                    return;
                }

                b.OnBroken();
                Self.Camera.Shake(
                    b.CameraShakeMagnitude,
                    b.CameraShakeFrequency,
                    b.CameraShakeDuration
                );
            }
        }

        /// <summary>
        /// Changes to the ledge-grabbing state and returns true, if there is
        /// a valid ledge to grab
        /// </summary>
        /// <returns></returns>
        protected bool TryGrabLedge()
        {
            if (!Self.LedgeDetector.LedgeDetected)
                return false;

            if (Self.LedgeDetector.IsBlocked)
                return false;

            ChangeState<PlayerLedgeGrabState>();
            return true;
        }
    }
}