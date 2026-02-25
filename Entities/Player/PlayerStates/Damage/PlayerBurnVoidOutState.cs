using Godot;

namespace FastDragon
{
    public partial class PlayerBurnVoidOutState : PlayerState
    {
        public override bool Invincible => true;

        private double _timer;

        public override void OnStateEntered()
        {
            var animation = Self.Animator.GetAnimation("BurnVoidOut");
            _timer = animation.Length;
            Self.Animator.Play("BurnVoidOut");
        }

        public override void _PhysicsProcess(double delta)
        {
            _timer -= delta;
            if (_timer <= 0)
            {
                if (Self.Health > 0)
                    Self.SafeGround.ReturnToLastSafeGround();
                else
                    Self.Die();
            }
        }
    }
}