using Godot;

namespace FastDragon
{
    public partial class BonkDecal : Node3D
    {
        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += Reset;
            Reset();
        }

        private void Reset()
        {
            Visible = false;
        }

        public void Play(KinematicCollision3D collision)
        {
            GlobalRotation = collision.GetNormal().ForwardToEulerAnglesRad();
            GlobalPosition = collision.GetPosition() + (collision.GetNormal() * 0.1f);
            Visible = true;
        }
    }
}