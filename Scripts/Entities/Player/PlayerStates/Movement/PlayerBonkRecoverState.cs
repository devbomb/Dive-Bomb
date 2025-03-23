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

            _timer = Player.Bonk.RecoverDuration;
            StartRecoveryAnimation();
        }

        public override void _PhysicsProcess(double deltaD)
        {
            _timer -= (float)deltaD;

            if (_timer <= 0)
                _player.ChangeState<PlayerStandState>();
        }

        private void StartRecoveryAnimation()
        {
            float len = _player.Animator.GetAnimation(RecoverAnim).Length;
            float speed = len / Player.Bonk.RecoverDuration;
            _player.Animator.Play(RecoverAnim, customSpeed: speed, customBlend: 0);
            _player.Animator.Advance(0);
        }
    }
}