using Godot;
using System;
using System.Linq;

namespace FastDragon
{
    public partial class PowerTriggerZone : Area3D
    {
        [Export] public string target;
        [Export] public bool Invert;

        private IPowerable _targetPowerable;
        private bool _playerInside;

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += Reset;

            Callable.From(() =>
            {
                _targetPowerable = this.FindNodeByTargetName<IPowerable>(target);
                Reset();
            }).CallDeferred();
        }

        private void Reset()
        {
            _playerInside = false;
            _targetPowerable.ForceSetPowered(PossiblyInvert(false));
        }

        public override void _PhysicsProcess(double delta)
        {
            bool playerWasInside = _playerInside;
            _playerInside = this.GetOverlappingBodiesResetSafe()
                .OfType<Player>()
                .Any();

            if (_playerInside != playerWasInside)
                _targetPowerable.SetPowered(PossiblyInvert(_playerInside));
        }

        private bool PossiblyInvert(bool value)
        {
            return Invert
                ? !value
                : value;
        }
    }
}
