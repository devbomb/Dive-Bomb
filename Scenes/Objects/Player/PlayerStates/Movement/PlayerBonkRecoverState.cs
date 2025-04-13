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
            _player.Velocity = Vector3.Zero;

            _timer = _player.Animator.GetAnimation(RecoverAnim).Length;
            _player.Animator.Play(RecoverAnim, customBlend: 0);
            _player.Animator.Advance(0);
        }

        public override void _PhysicsProcess(double deltaD)
        {
            _timer -= (float)deltaD;

            if (!_player.Animator.IsPlaying())
                _player.ChangeState<PlayerStandState>();
        }
    }
}