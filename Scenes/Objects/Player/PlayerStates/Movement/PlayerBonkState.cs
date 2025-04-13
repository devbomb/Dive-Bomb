using System;
using Godot;

namespace FastDragon
{
    public partial class PlayerBonkState : PlayerState
    {
        public override void OnStateEntered()
        {
            _player.Animator.Play("Bonk", 0);
            _player.Velocity = _player.GlobalForward() * -Player.Bonk.InitHSpeed;
            _player.Velocity += Vector3.Up * Player.Bonk.InitVSpeed;

            _player.Camera.Shake(
                magnitude: new Vector2(0, 1),
                frequency: new Vector2(0, 15),
                duration: 0.25f
            );
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            // Slow down horizontally, but not vertically
            Vector3 newVel = _player.Velocity.Flattened();
            newVel = newVel.MoveToward(Vector3.Zero, Player.Bonk.Friction * delta);
            newVel.Y = _player.Velocity.Y;
            _player.Velocity = newVel;

            ApplyGravity(delta, Player.Bonk.Gravity);

            _player.MoveAndSlide();

            if (_player.IsOnFloor())
            {
                _player.ChangeState<PlayerBonkRecoverState>();
                return;
            }
        }
    }
}