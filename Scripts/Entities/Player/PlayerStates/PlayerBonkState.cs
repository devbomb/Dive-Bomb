using System;
using Godot;

namespace FastDragon
{
    public partial class PlayerBonkState : PlayerState
    {
        public override bool AllowFlaming => false;

        private static readonly float BonkSpeed;
        private static readonly float BonkFriction;

        static PlayerBonkState()
        {
            (BonkSpeed, BonkFriction) =
                AccelMath.SpeedAndFrictionNeededForDistanceAndTime(
                    Player.Bonk.Distance,
                    Player.Bonk.Duration
                );
        }

        public override void OnStateEntered()
        {
            _player.Camera.ChangeState<OrbitCameraFreeState>();
            _player.Animator.Play("Bonk");
            _player.Velocity = _player.GlobalForward() * -BonkSpeed;
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            // Slow down horizontally, but not vertically
            Vector3 newVel = _player.Velocity.Flattened();
            newVel = newVel.MoveToward(Vector3.Zero, BonkFriction * delta);
            newVel.Y = _player.Velocity.Y;
            _player.Velocity = newVel;

            ApplyGravity(delta, Player.Default.Gravity);

            _player.MoveAndSlide();

            if (_player.Velocity.Flattened() == Vector3.Zero)
            {
                _player.ChangeState<PlayerWalkState>();
                return;
            }
        }
    }
}