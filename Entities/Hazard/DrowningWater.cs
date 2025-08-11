using Godot;

namespace FastDragon
{
    public partial class DrowningWater : Area3D
    {
        public override void _Ready()
        {
            BodyEntered += OnBodyEntered;
        }

        public void OnBodyEntered(Node3D body)
        {
            if (body is Player p)
            {
                // Only damage the player if they're vulnerable.
                p.TryDamage();

                // Always switch to the drowning state regardless of if damage
                // was dealt.
                p.ChangeState<PlayerDrownState>();
            }
        }
    }
}