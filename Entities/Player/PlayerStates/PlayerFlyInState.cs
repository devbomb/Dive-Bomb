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
            _endPos = Self.GlobalPosition;
            _endRotRad = Self.GlobalRotation;
            _timer = 0;

            Self.Animator.Play(
                "ParachuteOpen",
                customBlend: 0.25f
            );
            Self.Animator.Queue("Parachute");

            Self.GlobalPosition += Vector3.Up * Self.FlyInHeight;
            Self.GlobalPosition -= Self.GlobalForward() * Self.FlyInDistance;
            Self.GlobalRotation = Vector3.Zero;
            Self.ResetPhysicsInterpolation3D();

            Self.CameraFocus.Reset();

            _startPos = Self.GlobalPosition;
            _startRotRad = Self.GlobalRotation;

            Self.Camera.OrbitDistance = PortalLoadingScreen.CameraDist;
            Self.Camera.OrbitYawRad = PortalLoadingScreen.EnterLevelCameraYawRad;
            Self.Camera.OrbitPitchRad = PortalLoadingScreen.EnterLevelCameraPitchRad;
            Self.Camera.ApplyAnglesAndDistance();
            Self.Camera.ResetPhysicsInterpolation3D();

            // Pause the game while flying in.  This way, the fly-in won't
            // affect cycles.
            GetTree().Paused = true;
            Self.ProcessMode = Node.ProcessModeEnum.Always;
        }

        public override void OnStateExited()
        {
            GetTree().Paused = false;
            Self.ProcessMode = Node.ProcessModeEnum.Inherit;
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            _timer += delta;
            float t = _timer / Self.FlyInDuration;
            Self.GlobalPosition = _startPos.Lerp(_endPos, DecelerateToOne(t));
            Self.GlobalRotation = _startRotRad.LerpEulerRadSinusoidal(_endRotRad, t);

            Self.Camera.OrbitYawRad = Mathf.LerpAngle(
                PortalLoadingScreen.EnterLevelCameraYawRad,
                _endRotRad.Y,
                t
            );

            Self.Camera.OrbitPitchRad = Mathf.LerpAngle(
                PortalLoadingScreen.EnterLevelCameraPitchRad,
                0,
                t
            );

            if (_timer > Self.FlyInDuration)
            {
                Self.ChangeState<PlayerFlyInLandState>();
            }
        }

        private static float DecelerateToOne(float t)
        {
            float foo = t - 1;
            foo *= foo;
            return -foo + 1;
        }
    }
}