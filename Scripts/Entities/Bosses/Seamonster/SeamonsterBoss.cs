using System;
using Godot;

namespace FastDragon
{
    public partial class SeamonsterBoss : CharacterBody3D
    {
        [Export] public int[] PhaseMaxHealths = new int[0];
        [Export] public ReturnHomeVortex ReturnHomeVortex;

        public int CurrentHealth => _health.CurrentHealth;
        public int MaxHealth => _health.MaxHealth;

        private AnimationTree _animationTree => GetNode<AnimationTree>("%AnimationTree");

        private BossHealth _health;
        private BreakableArea3D _weakPoint => GetNode<BreakableArea3D>("%WeakPoint");

        private readonly StateMachine _stateMachine = new StateMachine(typeof(SeamonsterBossState));
        private readonly Random _rng = new Random();

        private Transform3D _currentSpawnPos;
        private PowerOrb[] _chosenPowerOrbs = new PowerOrb[0];

        private KrackenSplashTentacle _leftSplashTentacle => GetNode<KrackenSplashTentacle>("%LeftSplashTentacle");
        private KrackenSplashTentacle _rightSplashTentacle => GetNode<KrackenSplashTentacle>("%RightSplashTentacle");

        public override void _Ready()
        {
            AddChild(_stateMachine);

            SignalBus.Instance.LevelReset += Respawn;

            // Defer respawning to ensure the player is ready, since we'll be
            // hijacking the camera.
            _stateMachine.ChangeState<WaitingToRespawn>();

            _health = new BossHealth(PhaseMaxHealths);
        }

        public void Respawn()
        {
            _health = new BossHealth(PhaseMaxHealths);

            _currentSpawnPos = InitialSpawnPoint.GlobalTransform;
            _stateMachine.ChangeState<Submerged>();

            UseBossCameraAngle();
        }

        private void UseCameraAngle(Transform3D position)
        {
            GetTree()
                .FindNode<PlayerCamera>()
                .FixPosition(position);
        }

        private void UseBossCameraAngle()
        {
            GetTree()
                .FindNode<PlayerCamera>()
                .FixPosition(CameraFixPoint.GlobalTransform);
        }

        private void UseOverheadCameraAngle()
        {
            GetTree()
                .FindNode<PlayerCamera>()
                .FixPosition(AcidSplashesCameraPoint.GlobalTransform);
        }

        private void ShowWeakPoint(bool shouldShow)
        {
            _weakPoint.Disabled = !shouldShow;
            _weakPoint.Visible = shouldShow;
        }

        private void PlayAnimation(string animationName)
        {
            var playback = (AnimationNodeStateMachinePlayback)_animationTree.Get("parameters/playback");
            playback.Start(animationName);
        }

        private string CurrentAnimation()
        {
            var playback = (AnimationNodeStateMachinePlayback)_animationTree.Get("parameters/playback");
            return playback.GetCurrentNode();
        }

        private abstract partial class SeamonsterBossState : State
        {
            protected SeamonsterBoss _self => _stateMachine.GetParent<SeamonsterBoss>();
        }

        private partial class WaitingToRespawn : SeamonsterBossState
        {
            private int _timer;

            public override void OnStateEntered()
            {
                _timer = 2;
                _self.Visible = false;
            }

            public override void OnStateExited()
            {
                _self.Visible = true;
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer--;

                if (_timer <= 0)
                    _self.Respawn();
            }
        }
    }
}