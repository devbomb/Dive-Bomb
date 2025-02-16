using Godot;

namespace FastDragon
{
    public partial class ReturnHomePlatform : Node3D
    {
        [Export] public float ExitHeight = 15;

        private ReturnHomeVortex _vortex => GetNode<ReturnHomeVortex>("%Vortex");

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += OnLevelReset;
            _vortex.ExitHeight = ExitHeight;
        }

        private void OnLevelReset()
        {
        }
    }
}