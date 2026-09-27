using Godot;

namespace FastDragon
{
    // HACK: This state abuses inheritance to have "substates".
    // Please forgive me.
    //
    // TODO: Refactor StateMachine to allow for proper nested state machines.
    public partial class PlayerDamageFlipState : PlayerState
    {
        public override bool Invincible => true;
        public override bool PauseDamageCooldownTimer => true;

        public override void OnStateEntered()
        {
            ChangeState<PlayerDamageFlipSubstateFalling>();
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            ApplyGravity(delta);
            DecelerateHSpeedToZero(delta);
            Self.MoveAndSlide();
        }

        public override void OnStateExited(IState nextState)
        {
            // Reset the the animator to avoid the "360 degree wrap around" effect.
            //
            // This animation ends with the player's rotation being 360 degrees,
            // which is NOT technically the same as 0 degrees!
            // When Godot blends this animation with another, it tweens the
            // player's rotation from 360 all the way back down to 0, instead of
            // noticing that the rotation is congruent.
            //
            // Resetting the animator immediately sets the rotation back to 0
            // degrees, thus avoiding the effect at the cost of forgoing
            // animation blending.
            if (nextState is not PlayerDamageFlipState)
            {
                Self.Animator.Play("RESET", 0);
                Self.Animator.Seek(0, true);
            }
        }

        private void DecelerateHSpeedToZero(float delta)
        {
            var v = Self.LocalVelocity.Flattened();
            v = v.MoveToward(Vector3.Zero, Player.Walk.Decel * delta);
            v.Y = Self.LocalVelocity.Y;

            Self.LocalVelocity = v;
        }

        private class PlayerDamageFlipSubstateFalling : PlayerDamageFlipState
        {
            private const float VSpeed = 5;
            private double _timer;

            public override void OnStateEntered()
            {
                _timer = Self.Animator.PlayGetLength("DamageFlip");
                Self.FlipDamageSound.Play();
                Self.VSpeed = VSpeed;
                Self.FSpeed = 0;

                Self.Camera.Shake(1.1f, 15, 0.5f);
            }

            public override void _PhysicsProcess(double delta)
            {
                base._PhysicsProcess(delta);

                _timer -= delta;
                if (Self.IsOnFloor() && _timer <= 0)
                    ChangeState<PlayerDamageFlipSubstateLanded>();
            }
        }

        private class PlayerDamageFlipSubstateLanded : PlayerDamageFlipState
        {
            private double _timer;

            public override void OnStateEntered()
            {
                _timer = Self.Animator.PlayGetLength("DamageFlip_Land");
            }

            public override void _PhysicsProcess(double delta)
            {
                base._PhysicsProcess(delta);

                _timer -= delta;
                if (Self.IsOnFloor() && _timer <= 0)
                {
                    if (Self.Health <= 0)
                        ChangeState<PlayerReachOutDeathState>();
                    else
                        ChangeState<PlayerWalkState>();
                }
            }
        }
    }
}