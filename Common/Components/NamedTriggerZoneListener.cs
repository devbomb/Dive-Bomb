using System.Linq;
using Godot;

namespace FastDragon
{
    [GlobalClass]
    public partial class NamedTriggerZoneListener : Node
    {
        [Signal] public delegate void NamedTriggerEnteredEventHandler();
        [Signal] public delegate void NamedTriggerExitedEventHandler();

        [Export] public string TriggerName;

        public bool PlayerInside { get; private set; }

        private NamedTriggerZone _trigger;

        public override void _Ready()
        {
            Callable.From(() =>
            {
                _trigger = GetTree()
                    .Root
                    .EnumerateDescendantsOfType<NamedTriggerZone>()
                    .FirstOrDefault(t => t.targetname == TriggerName);

                if (_trigger == null)
                    throw new System.Exception($"Can't find a trigger named {TriggerName}");
            }).CallDeferred();
        }

        public override void _PhysicsProcess(double delta)
        {
            bool wasInside = PlayerInside;
            PlayerInside = _trigger?.GetOverlappingBodiesResetSafe()
                ?.OfType<Player>()
                ?.Any() ?? false;

            if (wasInside != PlayerInside)
            {
                if (PlayerInside)
                    EmitSignal(SignalName.NamedTriggerEntered);
                else
                    EmitSignal(SignalName.NamedTriggerExited);
            }
        }
    }
}