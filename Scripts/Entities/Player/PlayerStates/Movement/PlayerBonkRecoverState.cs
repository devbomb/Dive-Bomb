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
            _player.Animator.Play(LandAnim);
            _player.Velocity = Vector3.Zero;

            _timer = Player.Bonk.RecoverDuration;
            _startedRecoverAnimation = false;
        }

        public override void _PhysicsProcess(double deltaD)
        {
            _timer -= (float)deltaD;

            if (!_player.Animator.IsPlaying() && !_startedRecoverAnimation)
            {
                _startedRecoverAnimation = true;
                float len = _player.Animator.GetAnimation(RecoverAnim).Length;
                float speed = len / _timer;
                _player.Animator.Play("BonkRecover", customSpeed: speed);
            }

            if (_timer <= 0)
                _player.ChangeState<PlayerStandState>();
        }
    }
}