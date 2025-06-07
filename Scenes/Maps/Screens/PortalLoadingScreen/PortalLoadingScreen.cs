using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class PortalLoadingScreen : Node3D
    {
        public static float EnterLevelCameraYawRad => Mathf.DegToRad(-180);
        public static float EnterLevelCameraPitchRad => Mathf.DegToRad(60);

        public static float ReturnHomeCameraYawRad => Mathf.DegToRad(180);
        public static float ReturnHomeCameraPitchRad => 0;

        public const float CameraDist = Player.Glide.CameraDistance;

        [Signal] public delegate void DoneSkippingEventHandler();
        [Signal] public delegate void DoneMovingToRestEventHandler();

        private float _cameraYawRad => _isReturningHome
            ? ReturnHomeCameraYawRad
            : EnterLevelCameraYawRad;

        private float _cameraPitchRad => _isReturningHome
            ? ReturnHomeCameraPitchRad
            : EnterLevelCameraPitchRad;

        private const float RestMoveDuration = 2;

        private const float CorrectionAnimationDuration = 1;
        private const float SkipDuration = 0.25f;
        private const float MinLoadingWaitTime = 1;

        private DirectionalLight3D _oldSun;
        private LoadingScreenParameters _parameters;

        private Node3D _playerModel => GetNode<Node3D>("%PlayerModel");
        private AnimationPlayer _playerAnimator => GetNode<AnimationPlayer>("%PlayerAnimator");

        private AudioStreamPlayer _windSound => GetNode<AudioStreamPlayer>("%WindSound");

        private OrbitCamera _camera => GetNode<OrbitCamera>("%OrbitCamera");
        private Node3D _cameraFocus => GetNode<Node3D>("%CameraFocus");
        private Node3D _cameraFocusRestPos => GetNode<Node3D>("%CameraFocusRestPos");

        private WorldEnvironment _worldEnv => GetNode<WorldEnvironment>("%WorldEnv");

        private bool _isReturningHome => _parameters.PreviousMapSceneFilePath != null;
        private Node3D _loadedScene;

        private StateMachine _stateMachine = new StateMachine(typeof(LoadingScreenState));

        public override void _Ready()
        {
            AddChild(_stateMachine);

            // Allow the player to be seen on top of the black fade curtain
            var visuals = this.EnumerateDescendantsOfType<VisualInstance3D>();
            foreach (var v in visuals)
            {
                v.SetLayerMaskValue(RenderLayer.VisibleInPortals, true);
            }
        }

        public void Initialize(LoadingScreenParameters parameters)
        {
            _parameters = parameters;
            Log.LoadingScreenStarted(
                parameters.PreviousMapSceneFilePath,
                parameters.TargetMapSceneFilePath,
                _isReturningHome,
                SaveFile.Current.TotalGemCount + SaveFile.Current.GemsSpent,
                SaveFile.Current.GemsSpent
            );

            _worldEnv.Environment = parameters.SkyBoxEnvironment;
            _oldSun = parameters.OldSun;
            _oldSun.SkyMode = DirectionalLight3D.SkyModeEnum.LightOnly;
            AddChild(_oldSun);

            _loadedScene = null;

            // Start loading the level in the background
            ResourceLoader.LoadThreadedRequest(parameters.TargetMapSceneFilePath);

            // Sync up the player's animation...
            _playerAnimator.Play(parameters.AnimationName);
            _playerAnimator.Seek(parameters.AnimationStartTime, true);

            // ...and then make it transition the animation used for loading
            if (!_isReturningHome)
            {
                _playerAnimator.Play("Glide", 0.25f);
            }
            else
            {
                _playerAnimator.Play("Levitate", 2);
            }

            // Put everything in the starting position
            _playerModel.GlobalRotation = parameters.PlayerStartRotRad;
            _cameraFocus.GlobalPosition = parameters.CameraFocusPos;
            _camera.OrbitDistance = parameters.CameraDist;
            _camera.OrbitYawRad = parameters.CameraYawRad;
            _camera.OrbitPitchRad = parameters.CameraPitchRad;

            // Start the animation
            _stateMachine.ChangeState<MovingToRest>();
            StartFadingWindIn();
        }

        public void OnAnimationsDone()
        {
            _stateMachine.ChangeState<WaitingForLoad>();
        }

        private void StartFadingWindIn()
        {
            const float duration = 2;

            _windSound.PitchScale = 0.01f;
            GetTree()
                .CreateTween()
                .TweenProperty(_windSound, "pitch_scale", 1, duration);
        }

        private void StartFadingWindOut()
        {
            const float duration = 1;

            _windSound.PitchScale = 1;
            GetTree()
                .CreateTween()
                .TweenProperty(_windSound, "pitch_scale", 0.01f, duration);
        }


        private void GoToTargetMap()
        {
            double animationPos = _playerAnimator.CurrentAnimationPosition;
            MapTransitionManager.Instance.ChangeSceneToNode(_loadedScene);

            var realPlayer = _loadedScene.FindNode<Player>();
            realPlayer.Animator.Play(_playerAnimator.AssignedAnimation, 0);
            realPlayer.Animator.Seek(_playerAnimator.CurrentAnimationPosition, true);
            realPlayer.Animator.Advance(0);

            if (_isReturningHome)
            {
                var portal = GetTargetPortal(_loadedScene);
                portal.PlayExitAnimation();
            }
            else
            {
                realPlayer.ChangeState<PlayerFlyInState>();
            }

            Log.LoadingScreenFinished();
        }

        private Portal GetTargetPortal(Node sceneRoot)
        {
            return sceneRoot
                .EnumerateDescendantsOfType<Portal>()
                .First(p => p.TargetMap == _parameters.PreviousMapSceneFilePath);

        }

        private abstract partial class LoadingScreenState : State
        {
            public virtual bool Skippable => false;
            protected PortalLoadingScreen _screen => _stateMachine.GetParent<PortalLoadingScreen>();

            public override void _Input(InputEvent ev)
            {
                if (InputService.SkipJustPressed(ev) && Skippable)
                {
                    ChangeState<Skipping>();
                }
            }
        }

        private partial class Skipping : LoadingScreenState
        {
            public override void OnStateEntered(State prevState)
            {
                GD.Print("Skipping animations");
                Log.LoadingScreenSkipped(prevState.GetType().Name);

                var tween = _screen.CreateTween();
                tween.TweenRotRadSinusoidal(_screen._playerModel, "global_rotation", Vector3.Zero, SkipDuration);
                tween.Parallel().TweenProperty(_screen._camera, "OrbitDistance", CameraDist, SkipDuration);
                tween.Parallel().TweenAngleRadSinusoidal(_screen._camera, "OrbitYawRad", _screen._cameraYawRad, SkipDuration);
                tween.Parallel().TweenAngleRadSinusoidal(_screen._camera, "OrbitPitchRad", _screen._cameraPitchRad, SkipDuration);
                tween.TweenCallback(Callable.From(() =>
                {
                    ChangeState<WaitingForAnimations>();
                    _screen.EmitSignal(PortalLoadingScreen.SignalName.DoneSkipping);
                }));
            }
        }

        private partial class MovingToRest : LoadingScreenState
        {
            public override bool Skippable => true;

            private Tween _tween;

            public override void OnStateEntered()
            {
                GD.Print("Started move-to-rest animation");

                _tween = _screen.CreateTween();
                _tween.TweenRotRadSinusoidal(_screen._playerModel, "global_rotation", Vector3.Zero, RestMoveDuration);
                _tween.Parallel().TweenProperty(_screen._cameraFocus, "global_position", _screen._cameraFocusRestPos.GlobalPosition, RestMoveDuration);
                _tween.Parallel().TweenProperty(_screen._camera, "OrbitDistance", CameraDist, RestMoveDuration);
                _tween.Parallel().TweenAngleRadSinusoidal(_screen._camera, "OrbitYawRad", _screen._cameraYawRad, RestMoveDuration);
                _tween.Parallel().TweenAngleRadSinusoidal(_screen._camera, "OrbitPitchRad", _screen._cameraPitchRad, RestMoveDuration);
                _tween.TweenCallback(Callable.From(() =>
                {
                    ChangeState<WaitingForAnimations>();
                    _screen.EmitSignal(PortalLoadingScreen.SignalName.DoneMovingToRest);
                }));
            }

            public override void OnStateExited()
            {
                _tween.Stop();
            }
        }

        private partial class WaitingForAnimations : LoadingScreenState {}

        private partial class WaitingForLoad : LoadingScreenState
        {
            public override void _Process(double delta)
            {
                string sceneFile = _screen._parameters.TargetMapSceneFilePath;
                var loadStatus = ResourceLoader.LoadThreadedGetStatus(sceneFile);

                switch (loadStatus)
                {
                    case ResourceLoader.ThreadLoadStatus.Loaded:
                    {
                        var packedScene = (PackedScene)ResourceLoader.LoadThreadedGet(sceneFile);
                        _screen._loadedScene = packedScene.Instantiate<Node3D>();

                        ChangeState<CorrectingAngle>();
                        break;
                    }

                    case ResourceLoader.ThreadLoadStatus.Failed:
                    case ResourceLoader.ThreadLoadStatus.InvalidResource:
                        throw new Exception($"Error loading scene: {loadStatus}");
                }
            }
        }

        private partial class CorrectingAngle : LoadingScreenState
        {
            public override void OnStateEntered()
            {
                GD.Print("Started correction animation");

                _screen.StartFadingWindOut();

                var tween = _screen.CreateTween();

                TweenSun(tween.Parallel());

                if (_screen._isReturningHome)
                {
                    TweenPlayerToPortal(tween.Parallel());
                }

                tween.TweenCallback(Callable.From(_screen.GoToTargetMap));
            }

            private void TweenSun(Tween tween)
            {
                float duration = CorrectionAnimationDuration;
                var newSun = _screen._loadedScene.EnumerateChildren()
                    .Where(n => n is DirectionalLight3D)
                    .Cast<DirectionalLight3D>()
                    .FirstOrDefault(l => l.SkyMode != DirectionalLight3D.SkyModeEnum.SkyOnly);
                // Why filter out SkyOnly?  Because in some levels, the desired
                // light direction is not necessarily the same as the sun's
                // position in the skybox.  In those levels, we use a SkyOnly
                // light to position the sun in the skybox, and then use a
                // separate light to set the light direction.
                //
                // We only care about tweening the light direction, because
                // a sudden change in light direction breaks the illusion,
                // whereas a sudden change in the skybox sun doesn't have nearly
                // the same shock.  Don't believe me?  Fire up Spyro 1 and enter
                // Wild Flight.  Did you notice that the sun disappearing?  No?
                // Exactly my point!

                if (newSun == null)
                    return;

                tween.TweenRotRadSinusoidal(_screen._oldSun, "rotation", newSun.Rotation, duration);

                var lightProperties = newSun.GetPropertyList()
                    .Select(x => (string)x["name"])
                    .Where(n => n.StartsWith("light_"))
                    .Where(n => n != "light_cull_mask");

                foreach (var propertyName in lightProperties)
                {
                    tween.Parallel().TweenProperty(
                        _screen._oldSun,
                        (string)propertyName,
                        newSun.Get(propertyName),
                        duration
                    );
                }
            }

            void TweenPlayerToPortal(Tween tween)
            {
                float duration = CorrectionAnimationDuration;

                var portal = _screen.GetTargetPortal(_screen._loadedScene);
                Vector3 portalRotRad = portal.GetGlobalTransformOutsideOfTree().Basis.GetEuler();

                tween.TweenRotRadSinusoidal(_screen._playerModel, "global_rotation", portalRotRad, duration);
                tween.Parallel().TweenProperty(_screen._camera, "OrbitDistance", CameraDist, duration);
                tween.Parallel().TweenAngleRadSinusoidal(_screen._camera, "OrbitYawRad", portalRotRad.Y + Mathf.DegToRad(180), duration);
                tween.Parallel().TweenAngleRadSinusoidal(_screen._camera, "OrbitPitchRad", _screen._cameraPitchRad, duration);
            }
        }
    }
}