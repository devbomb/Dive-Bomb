using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FastDragon
{
    public partial class PlayerRollState : PlayerState
    {
        private const float RollingRadius = 0.5f;
        private const float RollingCircumference = 2 * Mathf.Pi * RollingRadius;

        private const float CameraLagDuration = 0.5f;

        private float _timer;
        private bool _isGroundRoll;

        private List<IBreakable> _brokenObjects = new();
        private List<IBreakable> _unbrokenObjects = new();
        private List<IBreakable> _detectedObjects = new();

        public override void OnStateEntered(IState oldState)
        {
            Self.Animator.Play("Roll");
            Self.RollThuum.Visible = true;

            _timer = 0;
            _isGroundRoll = !(oldState is PlayerDiveState);
            Self.LocalVelocity = Self.GlobalForward() * Player.Roll.InitialSpeed;
            Self.Camera.Lag(CameraLagDuration);

            Self.RollSoundPlayer.Play(0.025f);
        }

        public override void OnStateExited()
        {
            Self.Animator.SpeedScale = 1;
            Self.RollThuum.Visible = false;
            Self.RollDust.Emitting = false;
        }

        public override void _Process(double deltaD)
        {
            float scale = Self.LocalVelocity.Length() / RollingCircumference;
            Self.Animator.SpeedScale = scale;

            float speedPercent = Mathf.InverseLerp(
                Player.Roll.MinSpeed,
                Player.Roll.InitialSpeed,
                Self.LocalVelocity.Length()
            );
            Self.RollThuum.Transparency = 1f - speedPercent;

            Self.RollDust.Emitting = Self.IsOnFloor();
        }

        public override void _Input(InputEvent ev)
        {
            if (InputService.JumpJustPressed(ev))
            {
                Self.ChangeState<PlayerWalkJumpState>();
                return;
            }
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            ApplyGravity(delta);

            _timer += delta;

            if (_isGroundRoll && _timer < Player.Roll.RedirectTimeWindow)
            {
                float speed = Self.FSpeed;
                RotateInstantlyTowardLeftStick();
                Self.FSpeed = speed;
            }

            float maxSpeed = _timer < Player.Roll.FrictionlessDuration
                ? Player.Roll.InitialSpeed
                : Player.Roll.MinSpeed;

            AccelerateWithLeftStickAgainstDrag(
                maxSpeed,
                Player.Roll.MinAccel,
                Player.Roll.MaxAccel,
                delta
            );

            RotateInstantlyTowardVelocity();

            _brokenObjects.Clear();
            _unbrokenObjects.Clear();
            _detectedObjects.Clear();

            MoveAndSlideBreakingObjects_Roll(delta);

            foreach (var b in _brokenObjects)
            {
                b.OnRolledInto();
                Break(b);
            }

            foreach (var b in _unbrokenObjects)
                b.OnBreakRejected();

            // Apply an extra-wide hitbox to catch objects that the player
            // barely grazes past without touching.
            _detectedObjects.AddRange(_brokenObjects);
            _detectedObjects.AddRange(_unbrokenObjects);

            ApplyHitboxToBreakableObjects(
                Self.RollExtraHitbox,
                _detectedObjects,
                b => b.VulnerableToRoll,
                b => b.OnRolledInto()
            );

            // It's possible for the objects hit by the hitbox to change the
            // current state(IE: because you bonked, or you freed a fairy).
            // We don't want to overwrite that state change if
            // it happened to happen on the final frame of the timer.
            if (!IsCurrent)
                return;

            Self.SafeGround.UpdateLastSafeGroundPos();

            if (_timer >= Player.Roll.Duration)
            {
                if (Self.IsOnFloor())
                {
                    Self.ChangeState<PlayerWalkState>();
                }
                else
                {
                    Self.ChangeState<PlayerFlopState>();
                }

                return;
            }
        }

        private bool MoveAndSlideBreakingObjects_Roll(float delta)
        {
            Vector3 prevVel = Self.Velocity;
            Self.MoveAndSlideEx(OnCollision);

            int numCollisions = Self.GetSlideCollisionCount();
            if (_brokenObjects.Any(b => b.CausesBonk))
            {
                return Bonk();
            }

            if (DeceleratedEnoughToBonk(prevVel, Self.Velocity))
            {
                return Bonk();
            }

            return false;

            bool Bonk()
            {
                Self.ChangeState<PlayerBonkState>();
                return true;
            }
        }

        private MoveAndSlideExResponse OnCollision(KinematicCollision3D collision)
        {
            var hitObject = collision.GetCollider();

            if (hitObject is not IBreakable b)
                return MoveAndSlideExResponse.Slide;

            if (!b.VulnerableToRoll)
            {
                _unbrokenObjects.Add(b);
                return MoveAndSlideExResponse.Slide;
            }

            _brokenObjects.Add(b);

            if (b.CausesBonk)
                return MoveAndSlideExResponse.Stop;

            return MoveAndSlideExResponse.Ignore;
        }

        private void Break(IBreakable b)
        {
            b.OnBroken();
            Self.Camera.Shake(
                b.CameraShakeMagnitude,
                b.CameraShakeFrequency,
                b.CameraShakeDuration
            );
        }

        private void AccelerateWithLeftStickAgainstDrag(
            float maxSpeed,
            float minAccel,
            float maxAccel,
            float delta
        )
        {
            Vector3 leftStick3D = LeftStick3D();
            Vector3 flatVel = Self.LocalVelocity.Flattened();

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
                flatVel += Self.GlobalForward() * accel * delta;
            }
            else
            {
                flatVel += leftStick3D.Normalized() * accel * delta;
            }

            // Save it
            flatVel.Y = Self.LocalVelocity.Y;
            Self.LocalVelocity = flatVel;
        }
    }
}

