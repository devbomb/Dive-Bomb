using Godot;

namespace FastDragon
{
    public partial class PlayerBonkRecoverState : PlayerState
    {
        private const string RecoverAnim = "BonkRecover";

        private double _timer;

        public override void OnStateEntered()
        {
            Self.LocalVelocity = Vector3.Down;

            _timer = Self.Animator.GetAnimation(RecoverAnim).Length;
            Self.Animator.Play(RecoverAnim, customBlend: 0);
            Self.Animator.Advance(0);
        }

        public override void _PhysicsProcess(double deltaD)
        {
            Self.MoveAndSlide();

            _timer -= (float)deltaD;
            if (_timer <= 0)
                Self.ChangeState<PlayerStandState>();
        }
    }
}