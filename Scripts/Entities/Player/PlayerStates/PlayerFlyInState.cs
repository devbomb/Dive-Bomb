using Godot;

namespace FastDragon
{
    public partial class PlayerFlyInState : PlayerState
    {
        public override bool Invincible => true;
        public override bool DisableCameraInput => true;
        public override bool UseMario64CameraFocus => false;

        private Vector3 _startPos;
        private Vector3 _startRotRad;

        private Vector3 _endPos;
        private Vector3 _endRotRad;
        private float _timer;

        public override void OnStateEntered()
        {
            _endPos = _player.GlobalPosition;
            _endRotRad = _player.GlobalRotation;
            _timer = 0;

            _player.Animator.Play(
                "Roll",
                customBlend: 0.25f,
                customSpeed: 2);

            _player.GlobalPosition += Vector3.Up * _player.FlyInHeight;
            _player.GlobalPosition -= _player.GlobalForward() * _player.FlyInDistance;
            _player.GlobalRotation = Vector3.Zero;
            _player.ResetPhysicsInterpolation();

            _player.CameraFocus.GlobalPosition = _player.CameraFocusRestPos.GlobalPosition;
            _player.CameraFocusInterpolator.ResetPhysicsInterpolation();

            _startPos = _player.GlobalPosition;
            _startRotRad = _player.GlobalRotation;

            _player.Camera.OrbitDistance = PortalLoadingScreen.CameraDist;
            _player.Camera.OrbitYawRad = PortalLoadingScreen.EnterLevelCameraYawRad;
            _player.Camera.OrbitPitchRad = PortalLoadingScreen.EnterLevelCameraPitchRad;
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            _timer += delta;
            float t = _timer / _player.FlyInDuration;
            _player.GlobalPosition = _startPos.Lerp(_endPos, t);
            _player.GlobalRotation = _startRotRad.LerpEulerRadSinusoidal(_endRotRad, t);

            _player.Camera.OrbitYawRad = Mathf.LerpAngle(
                PortalLoadingScreen.EnterLevelCameraYawRad,
                _endRotRad.Y,
                t
            );

            _player.Camera.OrbitPitchRad = Mathf.LerpAngle(
                PortalLoadingScreen.EnterLevelCameraPitchRad,
                0,
                t
            );

            if (_timer > _player.FlyInDuration)
            {
                _player.Respawn();
            }
        }
    }
}