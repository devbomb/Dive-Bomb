using System;
using Godot;

namespace FastDragon
{
    public partial class PlayerBonkState : PlayerState
    {

        public override void OnStateEntered()
        {
            Self.Animator.Play("Bonk", 0);
            Self.LocalVelocity = Self.GlobalForward() * -Player.Bonk.InitHSpeed;
            Self.LocalVelocity += Vector3.Up * Player.Bonk.InitVSpeed;

            Self.BonkSoundPlayer.Play();

            Self.Camera.Shake(
                magnitude: new Vector2(0, 1),
                frequency: new Vector2(0, 15),
                duration: 0.25f
            );
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            // Slow down horizontally, but not vertically
            Vector3 newVel = Self.LocalVelocity.Flattened();
            newVel = newVel.MoveToward(Vector3.Zero, Player.Bonk.Friction * delta);
            newVel.Y = Self.LocalVelocity.Y;
            Self.LocalVelocity = newVel;

            ApplyGravity(delta, Player.Bonk.Gravity);

            Self.MoveAndSlide();

            if (Self.IsOnFloor())
            {
                Self.ChangeState<PlayerBonkRecoverState>();
                return;
            }
        }
    }
}