using System;
using Godot;

namespace FastDragon
{
    public partial class ReturnHomePlatform : StaticBody3D, IBreakable
    {
        public bool VulnerableToRoll => CanBreak() && _stateMachine.CurrentState is Closed;
        public bool VulnerableToKick => false;

        [Export] public float ExitHeight = 15;

        private Node3D _crystalModel => GetNode<Node3D>("%CrystalModel");
        private CollisionShape3D _crystalShape => GetNode<CollisionShape3D>("%CrystalShape");
        private ReturnHomeVortex _vortex => GetNode<ReturnHomeVortex>("%Vortex");
        private Node3D _requirementsDisplay => GetNode<Node3D>("%RequirementsDisplay");
        private Node3D _requirementsPivot => GetNode<Node3D>("%RequirementsPivot");

        private GpuParticles3D _shatterParticles => GetNode<GpuParticles3D>("%ShatterParticles");

        private readonly StateMachine _stateMachine = new StateMachine(typeof(State<ReturnHomePlatform>));

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
            if (timeTrialManager.IsTimeTrialMode)
            {
                if (timeTrialManager.RequirementsMet())
                    _stateMachine.ChangeState<Open>();
                else
                    _stateMachine.ChangeState<Closed>();
            }
            else
            {
                // TODO: Remember if the player has broken it before
                _stateMachine.ChangeState<Open>();
            }
        }

        public void OnBroken()
        {
            _stateMachine.ChangeState<Opening>();
        }

        private bool CanBreak()
        {
            return GetTree().FindNode<TimeTrialManager>().RequirementsMet();
        }

        private void SetRequirementsVisible(bool visible)
        {
            foreach (var m in Enum.GetValues<TimeTrialCategory>())
                _requirementsDisplay.GetNode<Node3D>(m.ToString()).Visible = !visible;

            var timeTrialManager = GetTree().FindNode<TimeTrialManager>();
            var currentMode = timeTrialManager?.Mode;

            if (currentMode != null)
                _requirementsDisplay.GetNode<Node3D>(currentMode.ToString()).Visible = visible;
        }

        private partial class Closed : State<ReturnHomePlatform>
        {
            public override void OnStateEntered()
            {
                Self._crystalModel.Visible = true;
                Self._crystalShape.Disabled = false;
                Self.SetRequirementsVisible(true);
            }

            public override void OnStateExited()
            {
                Self._crystalModel.Visible = false;
                Self._crystalShape.Disabled = true;
                Self.SetRequirementsVisible(false);
            }

            public override void _PhysicsProcess(double deltaD)
            {
                var lookPoint = GetTree().Root.GetCamera3D().GlobalPosition;
                lookPoint.Y = Self._requirementsPivot.GlobalPosition.Y;

                if (Self._requirementsPivot.GlobalPosition.DistanceTo(lookPoint) > 0.1f)
                    Self._requirementsPivot.LookAt(lookPoint);
            }
        }

        private partial class Opening : State<ReturnHomePlatform>
        {
            private const float Duration = 4f / 60;
            private const float HitStopDuration = 0.19f;

            private float _timer;

            public override void OnStateEntered()
            {
                _timer = Duration;
                Self._shatterParticles.Restart();
                Self._shatterParticles.Emitting = true;
                HitStopManager.Instance.StopFor(HitStopDuration);
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer -= (float)deltaD;

                if (_timer <= 0)
                    ChangeState<Open>();
            }
        }

        private partial class Open : State<ReturnHomePlatform>
        {
            public override void OnStateEntered()
            {
                Self._vortex.IsActive = true;
            }

            public override void OnStateExited()
            {
                Self._vortex.IsActive = false;
            }
        }
    }
}