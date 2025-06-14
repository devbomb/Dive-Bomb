using Godot;

namespace FastDragon
{
    public partial class PlayerBonkRecoverState : PlayerState
    {
        private const string LandAnim = "BonkLand";
        private const string RecoverAnim = "BonkRecover";

        private float _timer;
        private bool _startedRecoverAnimation;

        public override void OnStateEntered()
        {
            Self.Velocity = Vector3.Zero;

            _timer = Self.Animator.GetAnimation(RecoverAnim).Length;
            Self.Animator.Play(RecoverAnim, customBlend: 0);
            Self.Animator.Advance(0);
        }

        public override void _PhysicsProcess(double deltaD)
        {
            _timer -= (float)deltaD;

            if (!Self.Animator.IsPlaying())
                Self.ChangeState<PlayerStandState>();
        }
    }
}