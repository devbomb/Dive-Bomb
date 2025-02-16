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
                case TimeTrialManager.TimeTrialMode.FairyPercent:
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
                case TimeTrialManager.TimeTrialMode.FairyPercent:
                {
                    var saveFile = SaveFile.Current;
                    var mapEntry = AtlasCache.Instance.GetEntry(saveFile.CurrentMap);
                    int fairiesFound = saveFile.CurrentMapProgress.CollectedFairies.Count;

                    return fairiesFound >= mapEntry.TotalFairiesInLevel;
                }

                default: return true;
            }
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
            }

            public override void OnStateExited()
            {
                _self._crystalModel.Visible = false;
                _self._crystalShape.Disabled = true;
            }
        }

        private partial class Opening : PlatformState
        {
            private const float Duration = 0.2f;
            private float _timer;

            public override void OnStateEntered()
            {
                _timer = Duration;
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