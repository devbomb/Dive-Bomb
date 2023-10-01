using Godot;

namespace FastDragon
{
    public partial class DeathBarrier : Area3D
    {
        public void OnBodyEntered(Node3D body)
        {
            if (body is Player)
                SignalBus.Instance.EmitLevelReset();
        }
    }
}