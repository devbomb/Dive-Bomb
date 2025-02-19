using Godot;
using System.Collections.Generic;

namespace FastDragon
{
    public partial class PlayerDiveState : PlayerState
    {
        public override bool DisableCameraInput => _redirectTimer <= 0;

        private float _redirectTimer;
        private float _targetCameraYawRad;
        private float _startY;

        private List<IBreakable> _brokenObjects = new List<IBreakable>();

        public override void OnStateEntered()
        {
            _player.Animator.Play("Dive");
            _player.VSpeed = Player.Dive.InitialVSpeed;
            _player.FSpeed = Player.Dive.FSpeed;

            _redirectTimer = Player.Dive.RedirectTimeWindow;
            _targetCameraYawRad = _player.GlobalRotation.Y;

            _startY = _player.GlobalPosition.Y;
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
                var camera = _player.Camera;

                camera.OrbitDistance = MathUtils.DecayToward(
                    camera.OrbitDistance,
                    Player.Dive.CameraDistance,
                    Player.Dive.CameraDecayRate,
                    delta
                );

                camera.OrbitYawRad = AngleMath.DecayToward(
                    camera.OrbitYawRad,
                    _targetCameraYawRad,
                    Player.Dive.CameraDecayRate,
                    delta
                );

                // Allow the camera to look down at the player if they've
                // fallen below the height that they started the dive at.
                var transform = camera.GlobalTransform;
                transform = transform.LookingAt(_player.GlobalPosition);
                float pitchRad = _player.GlobalPosition.Y < _startY - 2
                    ? Mathf.Min(transform.Basis.GetEuler().X, Player.Dive.CameraPitchRad)
                    : Player.Dive.CameraPitchRad;

                camera.OrbitPitchRad = AngleMath.DecayToward(
                    camera.OrbitPitchRad,
                    pitchRad,
                    Player.Dive.CameraDecayRate / 4,
                    delta
                );

                camera.ApplyAnglesAndDistance();
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