using Godot;
using System.Collections.Generic;

namespace FastDragon
{
    public partial class PlayerDiveState : PlayerState
    {
        public override bool DisableCameraInput => _redirectTimer <= 0;

        private float _redirectTimer;
        private float _targetCameraYawRad;

        private List<IBreakable> _brokenObjects = new List<IBreakable>();

        public override void OnStateEntered()
        {
            _player.Animator.Play("Dive");
            _player.VSpeed = Player.Dive.InitialVSpeed;
            _player.FSpeed = Player.Dive.FSpeed;

            _redirectTimer = Player.Dive.RedirectTimeWindow;
            _targetCameraYawRad = _player.GlobalRotation.Y;
        }

        public override void OnStateExited()
        {
            ResetModelPitch();
        }

        public override void _Process(double deltaD)
        {
            float delta = (float)deltaD;

            AngleModelPitchWithVelocity(delta);

            if (_redirectTimer <= 0)
            {
                ContinuouslyRecenterCamera(
                    _targetCameraYawRad,
                    Player.Dive.CameraDistance,
                    Player.Dive.CameraPitchRad,
                    Player.Dive.CameraDecayRate,
                    delta
                );
            }
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            _redirectTimer -= delta;
            if (_redirectTimer > 0)
            {
                float speed = _player.FSpeed;
                RotateInstantlyTowardLeftStick();
                _player.FSpeed = speed;
            }

            if (_redirectTimer > -0.1f)
                _targetCameraYawRad = _player.GlobalRotation.Y;

            RotateTowardLeftStick(Player.Dive.TurnSpeedRad, delta);
            RedirectFSpeedTowardYaw();

            ApplyGravity(delta, Player.Dive.Gravity);

            _brokenObjects.Clear();
            bool bonked = MoveAndSlideBreakingObjects<IBreakable>(
                isBreakable: b => b.VulnerableToRoll,
                causesBonkWhenBroken: b => b.CausesBonk,
                brokenObjects: _brokenObjects,
                delta
            );

            foreach (var b in _brokenObjects)
            {
                b.OnRolledInto();
                Break(b);
            }

            if (bonked)
                return;

            // TODO: Don't apply the extra hitbox to objects that have already
            // been hit by the main hitbox
            ApplyExtraHitbox();

            if (_player.IsOnFloor())
            {
                _player.ChangeState<PlayerRollState>();
                return;
            }
        }

        private void ApplyExtraHitbox()
        {
            var bodies = _player.DiveExtraHitbox.GetOverlappingBodies();
            var areas = _player.DiveExtraHitbox.GetOverlappingAreas();

            foreach (var body in bodies)
            {
                if (body is IBreakable b && b.VulnerableToRoll)
                {
                    b.OnRolledInto();

                    if (b.VulnerableToRoll)
                        Break(b);
                }
            }

            foreach (var area in areas)
            {
                if (area is IBreakable b && b.VulnerableToRoll)
                {
                    b.OnRolledInto();

                    if (b.VulnerableToRoll)
                        Break(b);
                }
            }
        }

        private void Break(IBreakable b)
        {
            b.OnBroken();
            _player.Camera.Shake(
                b.CameraShakeMagnitude,
                b.CameraShakeFrequency,
                b.CameraShakeDuration
            );
        }
    }
}