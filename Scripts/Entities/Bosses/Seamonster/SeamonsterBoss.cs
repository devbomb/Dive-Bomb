using System;
using Godot;

namespace FastDragon
{
    public partial class SeamonsterBoss : CharacterBody3D
    {
        [Export] public int[] PhaseMaxHealths = new int[0];

        public int CurrentHealth => _health.CurrentHealth;
        public int MaxHealth => _health.MaxHealth;

        private BossHealth _health;
        private BreakableArea3D _weakPoint => GetNode<BreakableArea3D>("%WeakPoint");

        private readonly StateMachine _stateMachine = new StateMachine(typeof(SeamonsterBossState));
        private readonly Random _rng = new Random();

        private Transform3D _currentSpawnPos;
        private PowerOrb[] _chosenPowerOrbs = new PowerOrb[0];


        public override void _Ready()
        {
            AddChild(_stateMachine);

            SignalBus.Instance.LevelReset += Respawn;

            // Defer respawning to ensure the player is ready, since we'll be
            // hijacking the camera.
            CallDeferred(nameof(Respawn));
        }

        public void Respawn()
        {
            _health = new BossHealth(PhaseMaxHealths);

            _currentSpawnPos = InitialSpawnPoint.GlobalTransform;
            _stateMachine.ChangeState<Submerged>();

            UseBossCameraAngle();
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

        private abstract partial class SeamonsterBossState : State
        {
            protected SeamonsterBoss _self => _stateMachine.GetParent<SeamonsterBoss>();
        }
    }
}