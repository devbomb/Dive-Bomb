using System;
using Godot;

namespace FastDragon
{
    public partial class ReturnHomePlatform : StaticBody3D, IBreakable
    {
        public bool VulnerableToRoll => RequirementsMet() && _stateMachine.CurrentState is Closed;
        public bool VulnerableToKick => false;

        [Export] public float ExitHeight = 15;

        private Node3D _crystalModel => GetNode<Node3D>("%CrystalModel");
        private CollisionShape3D _crystalShape => GetNode<CollisionShape3D>("%CrystalShape");
        private ReturnHomeVortex _vortex => GetNode<ReturnHomeVortex>("%Vortex");
        private Node3D _requirementsDisplay => GetNode<Node3D>("%RequirementsDisplay");
        private Node3D _requirementsPivot => GetNode<Node3D>("%RequirementsPivot");

        private GpuParticles3D _shatterParticles => GetNode<GpuParticles3D>("%ShatterParticles");

        private readonly StateMachine _stateMachine = new StateMachine(typeof(PlatformState));

        public override void _Ready()
        {
            AddChild(_stateMachine);

            SignalBus.Instance.LevelReset += Reset;
            _vortex.ExitHeight = ExitHeight;
            Reset();
        }

        private void Reset()
        {
            var timeTrialManager = GetTree().FindNode<TimeTrialManager>();
            switch (timeTrialManager.Mode)
            {
                case TimeTrialCategory.FairyPercent:
                {
                    _stateMachine.ChangeState<Closed>();
                    break;
                }

                default:
                {
                    _stateMachine.ChangeState<Open>();
                    break;
                }
            }
        }

        public void OnBroken()
        {
            _stateMachine.ChangeState<Opening>();
        }

        private bool RequirementsMet()
        {
            var timeTrialManager = GetTree().FindNode<TimeTrialManager>();
            switch (timeTrialManager.Mode)
            {
                case TimeTrialCategory.FairyPercent:
                {
                    var saveFile = SaveFile.Current;
                    var mapEntry = AtlasCache.Instance.GetEntry(saveFile.CurrentMap);
                    int fairiesFound = saveFile.CurrentMapProgress.CollectedFairies.Count;

                    return fairiesFound >= mapEntry.TotalFairiesInLevel;
                }

                default: return true;
            }
        }

        private void SetRequirementsVisible(bool visible)
        {
            var timeTrialManager = GetTree().FindNode<TimeTrialManager>();
            var currentMode = timeTrialManager?.Mode ?? TimeTrialCategory.None;

            foreach (var m in Enum.GetValues<TimeTrialCategory>())
                _requirementsDisplay.GetNode<Node3D>(m.ToString()).Visible = !visible;

            _requirementsDisplay.GetNode<Node3D>(currentMode.ToString()).Visible = visible;
        }

        private partial class PlatformState : State
        {
            protected ReturnHomePlatform _self => _stateMachine.GetParent<ReturnHomePlatform>();
        }

        private partial class Closed : PlatformState
        {
            public override void OnStateEntered()
            {
                _self._crystalModel.Visible = true;
                _self._crystalShape.Disabled = false;
                _self.SetRequirementsVisible(true);
            }

            public override void OnStateExited()
            {
                _self._crystalModel.Visible = false;
                _self._crystalShape.Disabled = true;
                _self.SetRequirementsVisible(false);
            }

            public override void _PhysicsProcess(double deltaD)
            {
                var lookPoint = GetTree().Root.GetCamera3D().GlobalPosition;
                lookPoint.Y = _self._requirementsPivot.GlobalPosition.Y;

                if (_self._requirementsPivot.GlobalPosition.DistanceTo(lookPoint) > 0.1f)
                    _self._requirementsPivot.LookAt(lookPoint);
            }
        }

        private partial class Opening : PlatformState
        {
            private const float Duration = 4f / 60;
            private const float HitStopDuration = 0.19f;

            private float _timer;

            public override void OnStateEntered()
            {
                _timer = Duration;
                _self._shatterParticles.Restart();
                _self._shatterParticles.Emitting = true;
                HitStopManager.Instance.StopFor(HitStopDuration);
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer -= (float)deltaD;

                if (_timer <= 0)
                    ChangeState<Open>();
            }
        }

        private partial class Open : PlatformState
        {
            public override void OnStateEntered()
            {
                _self._vortex.IsActive = true;
            }

            public override void OnStateExited()
            {
                _self._vortex.IsActive = false;
            }
        }
    }
}