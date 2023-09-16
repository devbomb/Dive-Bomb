using System;
using Godot;

namespace FastDragon
{
    public partial class PlayerBonkState : PlayerState
    {
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
            // TODO: play the animation
            _player.Velocity = _player.GlobalForward() * -BonkSpeed;
        }

        public override void _Process(double deltaD)
        {
            float delta = (float)deltaD;

            _player.Velocity = _player.Velocity.MoveToward(
                Vector3.Zero,
                BonkFriction * delta
            );

            _player.MoveAndSlide();

            if (_player.Velocity == Vector3.Zero)
            {
                _player.ChangeState<PlayerWalkState>();
                return;
            }
        }
    }
}