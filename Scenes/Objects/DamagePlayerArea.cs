using System;
using Godot;

namespace FastDragon
{
    public partial class DamagePlayerArea : Area3D
    {
        [Signal] public delegate void PlayerDamagedEventHandler(Player player);
        [Export] public bool Enabled { get; set; }

        [Export] public PlayerDamageAnimation DamageAnimation { get; set; } = PlayerDamageAnimation.Flip;
        public enum PlayerDamageAnimation
        {
            Flip
        }

        public override void _PhysicsProcess(double delta)
        {
            if (!Enabled)
                return;

            foreach (var body in GetOverlappingBodies())
            {
                if (body is Player player)
                {
                    if (TryDamage(player))
                        EmitSignal(SignalName.PlayerDamaged, player);
                }
            }
        }

        private bool TryDamage(Player player)
        {
            switch (DamageAnimation)
            {
                case PlayerDamageAnimation.Flip: return player.TryDamage<PlayerDamageFlipState>();
                default: throw new Exception("Unknown damage animation");
            }
        }
    }
}