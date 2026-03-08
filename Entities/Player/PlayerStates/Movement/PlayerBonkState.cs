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

            // Inform all of the things we're touching that we bonked into them.
            // This assumes we got into this state as a result of colliding with
            // one of them.
            int numCollisions = Self.GetSlideCollisionCount();
            for (int i = 0; i < numCollisions; i++)
            {
                var collision = Self.GetSlideCollision(i);
                if (collision.GetCollider() is IBonkable b)
                {
                    b.OnBonked();
                }
            }
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