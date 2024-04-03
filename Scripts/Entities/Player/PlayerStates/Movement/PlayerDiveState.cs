using Godot;
using System.Collections.Generic;

namespace FastDragon
{
    public partial class PlayerDiveState : PlayerState
    {
        public override bool DisableCameraInput => _redirectTimer <= 0;

        private float _redirectTimer;

        private List<IRollable> _brokenObjects = new List<IRollable>();

        public override void OnStateEntered()
        {
            _player.Animator.Play("Dive");
            _player.VSpeed = Player.Dive.InitialVSpeed;
            _player.FSpeed = Player.Dive.FSpeed;

            _redirectTimer = Player.Dive.RedirectTimeWindow;
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

            RotateTowardLeftStick(Player.Dive.TurnSpeedRad, delta);
            RedirectFSpeedTowardYaw();

            ApplyGravity(delta, Player.Dive.Gravity);

            _brokenObjects.Clear();
            bool bonked = MoveAndSlideBreakingObjects<IRollable>(
                isBreakable: r => true,
                causesBonkWhenBroken: r => r.CausesBonk,
                brokenObjects: _brokenObjects,
                delta
            );

            foreach (var r in _brokenObjects)
            {
                OnBroke(r);
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
                if (body is IRollable r)
                {
                    OnBroke(r);
                }
            }

            foreach (var area in areas)
            {
                if (area is IRollable r)
                {
                    OnBroke(r);
                }
            }
        }

        private void OnBroke(IRollable r)
        {
            r.OnRolledInto();
        }
    }
}