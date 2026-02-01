using Godot;
using System;
using System.Linq;

namespace FastDragon
{
    public partial class PowerTriggerZone : Area3D
    {
        [Export] public string TargetId;
        [Export] public bool Invert;

        private IPowerable _target;
        private bool _playerInside;

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += Reset;

            Callable.From(() =>
            {
                _target = this.FindPowerable(TargetId);
                Reset();
            }).CallDeferred();
        }

        private void Reset()
        {
            _playerInside = false;
            _target.ForceSetPowered(PossiblyInvert(false));
        }

        public override void _PhysicsProcess(double delta)
        {
            bool playerWasInside = _playerInside;
            _playerInside = this.GetOverlappingBodiesResetSafe()
                .OfType<Player>()
                .Any();

            if (_playerInside != playerWasInside)
                _target.SetPowered(PossiblyInvert(_playerInside));
        }

        private bool PossiblyInvert(bool value)
        {
            return Invert
                ? !value
                : value;
        }
    }
}
