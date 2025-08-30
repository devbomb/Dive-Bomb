using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        [Export] public PackedScene LoadedScene = null;

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

        private bool _isReturningHome => _parameters.PreviousLevelScenePath != null;
        private Node3D _loadedSceneNode;

        private StateMachine _stateMachine = new StateMachine();

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
                parameters.PreviousLevelScenePath,
                parameters.TargetLevelScenePath,
                _isReturningHome,
                SaveFile.Current.TotalGemCount + SaveFile.Current.TotalGemsSpent,
                SaveFile.Current.TotalGemsSpent
            );

            _worldEnv.Environment = parameters.SkyBoxEnvironment;
            _oldSun = parameters.OldSun;
            _oldSun.SkyMode = DirectionalLight3D.SkyModeEnum.LightOnly;
            AddChild(_oldSun);

            _loadedSceneNode = null;

            // Start loading the level in the background
            // HACK: Using a C# thread instead of ResourceLoader.LoadThreadedRequest()
            // to avoid a non-deterministic deadlock.
            // See https://github.com/godotengine/godot/issues/107548
            new System.Threading.Thread(() =>
            {
                var loadedScene = ResourceLoader.Load<PackedScene>(parameters.TargetLevelScenePath);
                SetDeferred(nameof(LoadedScene), loadedScene);
            }).Start();

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


        private void GoToTargetLevel()
        {
            double animationPos = _playerAnimator.CurrentAnimationPosition;
            LevelTransitionManager.Instance.ChangeSceneToNode(_loadedSceneNode);

            var realPlayer = _loadedSceneNode.FindNode<Player>();
            realPlayer.Animator.Play(_playerAnimator.AssignedAnimation, 0);
            realPlayer.Animator.Seek(_playerAnimator.CurrentAnimationPosition, true);
            realPlayer.Animator.Advance(0);

            if (_isReturningHome)
            {
                var portal = GetTargetPortal(_loadedSceneNode);
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
                .First(p => p.TargetLevel == _parameters.PreviousLevelScenePath);

        }

        private abstract partial class LoadingScreenState : State<PortalLoadingScreen>
        {
            public virtual bool Skippable => false;

            public override void _Input(InputEvent ev)
            {
                if (InputService.SkipJustPressed(ev) && Skippable)
                {
                    ChangeState<Skipping>();
                }
            }
        }

        private class Skipping : LoadingScreenState
        {
            public override void OnStateEntered(IState prevState)
            {
                GD.Print("Skipping animations");
                Log.LoadingScreenSkipped(prevState.GetType().Name);

                var tween = Self.CreateTween();
                tween.TweenRotRadSinusoidal(Self._playerModel, "global_rotation", Vector3.Zero, SkipDuration);
                tween.Parallel().TweenProperty(Self._camera, "OrbitDistance", CameraDist, SkipDuration);
                tween.Parallel().TweenAngleRadSinusoidal(Self._camera, "OrbitYawRad", Self._cameraYawRad, SkipDuration);
                tween.Parallel().TweenAngleRadSinusoidal(Self._camera, "OrbitPitchRad", Self._cameraPitchRad, SkipDuration);
                tween.TweenCallback(Callable.From(() =>
                {
                    ChangeState<WaitingForAnimations>();
                    Self.EmitSignal(PortalLoadingScreen.SignalName.DoneSkipping);
                }));
            }
        }

        private class MovingToRest : LoadingScreenState
        {
            public override bool Skippable => true;

            private Tween _tween;

            public override void OnStateEntered()
            {
                GD.Print("Started move-to-rest animation");

                _tween = Self.CreateTween();
                _tween.TweenRotRadSinusoidal(Self._playerModel, "global_rotation", Vector3.Zero, RestMoveDuration);
                _tween.Parallel().TweenProperty(Self._cameraFocus, "global_position", Self._cameraFocusRestPos.GlobalPosition, RestMoveDuration);
                _tween.Parallel().TweenProperty(Self._camera, "OrbitDistance", CameraDist, RestMoveDuration);
                _tween.Parallel().TweenAngleRadSinusoidal(Self._camera, "OrbitYawRad", Self._cameraYawRad, RestMoveDuration);
                _tween.Parallel().TweenAngleRadSinusoidal(Self._camera, "OrbitPitchRad", Self._cameraPitchRad, RestMoveDuration);
                _tween.TweenCallback(Callable.From(() =>
                {
                    ChangeState<WaitingForAnimations>();
                    Self.EmitSignal(PortalLoadingScreen.SignalName.DoneMovingToRest);
                }));
            }

            public override void OnStateExited()
            {
                _tween.Stop();
            }
        }

        private class WaitingForAnimations : LoadingScreenState {}

        private class WaitingForLoad : LoadingScreenState
        {
            public override void _Process(double delta)
            {
                if (Self.LoadedScene != null)
                {
                    Self._loadedSceneNode = Self.LoadedScene.Instantiate<Node3D>();
                    ChangeState<CorrectingAngle>();
                }
            }
        }

        private class CorrectingAngle : LoadingScreenState
        {
            public override void OnStateEntered()
            {
                GD.Print("Started correction animation");

                Self.StartFadingWindOut();

                var tween = Self.CreateTween();

                TweenSun(tween.Parallel());

                if (Self._isReturningHome)
                {
                    TweenPlayerToPortal(tween.Parallel());
                }

                tween.TweenCallback(Callable.From(Self.GoToTargetLevel));
            }

            private void TweenSun(Tween tween)
            {
                float duration = CorrectionAnimationDuration;
                var newSun = Self._loadedSceneNode.EnumerateChildren()
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

                tween.TweenRotRadSinusoidal(Self._oldSun, "rotation", newSun.Rotation, duration);

                var lightProperties = newSun.GetPropertyList()
                    .Select(x => (string)x["name"])
                    .Where(n => n.StartsWith("light_"))
                    .Where(n => n != "light_cull_mask");

                foreach (var propertyName in lightProperties)
                {
                    tween.Parallel().TweenProperty(
                        Self._oldSun,
                        (string)propertyName,
                        newSun.Get(propertyName),
                        duration
                    );
                }
            }

            void TweenPlayerToPortal(Tween tween)
            {
                float duration = CorrectionAnimationDuration;

                var portal = Self.GetTargetPortal(Self._loadedSceneNode);
                Vector3 portalRotRad = portal.GetGlobalTransformOutsideOfTree().Basis.GetEuler();

                tween.TweenRotRadSinusoidal(Self._playerModel, "global_rotation", portalRotRad, duration);
                tween.Parallel().TweenProperty(Self._camera, "OrbitDistance", CameraDist, duration);
                tween.Parallel().TweenAngleRadSinusoidal(Self._camera, "OrbitYawRad", portalRotRad.Y + Mathf.DegToRad(180), duration);
                tween.Parallel().TweenAngleRadSinusoidal(Self._camera, "OrbitPitchRad", Self._cameraPitchRad, duration);
            }
        }
    }
}