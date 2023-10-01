using Godot;

namespace FastDragon
{
    public partial class DeathBarrier : Area3D
    {
        public override void _Ready()
        {
            BodyEntered += OnBodyEntered;
        }

        public void OnBodyEntered(Node3D body)
        {
            if (body is Player p)
                p.ChangeState<PlayerFallingToDeathState>();
        }
    }
}