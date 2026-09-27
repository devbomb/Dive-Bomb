using Godot;

namespace FastDragon
{
    public partial class PlayerReachOutDeathState : PlayerState
    {
        public override bool Invincible => true;
        private double _timer;

        public override void OnStateEntered()
        {
            _timer = Self.Animator.PlayGetLength("ReachOutDeath", 0);
            Self.LocalVelocity = Vector3.Zero;
        }

        public override void _PhysicsProcess(double delta)
        {
            ApplyGravity((float)delta);
            Self.MoveAndSlide();

            _timer -= delta;
            if (_timer <= 0)
                Self.Die();
        }
    }
}