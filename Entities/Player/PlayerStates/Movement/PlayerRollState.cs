using Godot;
using System;
using System.Collections.Generic;

namespace FastDragon
{
    public partial class PlayerRollState : PlayerState
    {
        private const float RollingRadius = 0.5f;
        private const float RollingCircumference = 2 * Mathf.Pi * RollingRadius;

        private const float CameraLagDuration = 0.5f;

        private float _timer;
        private bool _isGroundRoll;

        private List<IBreakable> _brokenObjects = new List<IBreakable>();
        private List<IBreakable> _unbrokenObjects = new List<IBreakable>();

        private AudioStreamPlayer _rollSoundPlayer => Self.GetNode<AudioStreamPlayer>("%RollSoundPlayer");
        private MeshInstance3D _thuum => Self.GetNode<MeshInstance3D>("%RollThuum");
        private GpuParticles3D _dust => Self.GetNode<GpuParticles3D>("%RollDust");

        public override void OnStateEntered(IState oldState)
        {
            Self.Animator.Play("Roll");
            _thuum.Visible = true;

            _timer = 0;
            _isGroundRoll = !(oldState is PlayerDiveState);
            Self.Velocity = Self.GlobalForward() * Player.Roll.InitialSpeed;
            Self.Camera.Lag(CameraLagDuration);

            _rollSoundPlayer.Play(0.025f);
        }

        public override void OnStateExited()
        {
            Self.Animator.SpeedScale = 1;
            _thuum.Visible = false;
            _dust.Emitting = false;
        }

        public override void _Process(double deltaD)
        {
            float scale = Self.Velocity.Length() / RollingCircumference;
            Self.Animator.SpeedScale = scale;

            float speedPercent = Mathf.InverseLerp(
                Player.Roll.MinSpeed,
                Player.Roll.InitialSpeed,
                Self.Velocity.Length()
            );
            _thuum.Transparency = 1f - speedPercent;

            _dust.Emitting = Self.IsOnFloor();
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

            MoveAndSlideBreakingObjects<IBreakable>(
                isVulnerable: b => b.VulnerableToRoll,
                causesBonkWhenBroken: b => b.CausesBonk,
                brokenObjects: _brokenObjects,
                unbrokenObjects: _unbrokenObjects,
                delta
            );

            foreach (var b in _brokenObjects)
            {
                b.OnRolledInto();
                Break(b);
            }

            foreach (var b in _unbrokenObjects)
                b.OnBreakRejected();

            // Apply an extra-wide hitbox to catch objects that the player
            // barely grazes past without touching.
            // TODO: Don't apply the extra hitbox to objects that have already
            // been hit by the main hitbox
            ApplyHitboxToBreakableObjects(
                Self.RollExtraHitbox,
                b => b.VulnerableToRoll,
                b => b.OnRolledInto()
            );

            // It's possible for the objects hit by the hitbox to change the
            // current state.  We don't want to overwrite that state change if
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
            Vector3 flatVel = Self.Velocity.Flattened();

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
            flatVel.Y = Self.Velocity.Y;
            Self.Velocity = flatVel;
        }
    }
}

