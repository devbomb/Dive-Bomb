using System;
using Godot;

namespace FastDragon
{
    public partial class BonkDecal : Node3D
    {
        public const float FadeSpeed = 0.75f;

        [ExportCategory("Internal")]
        [Export] public Decal Decal;

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
            Decal.AlbedoMix = 1;
        }

        public override void _Process(double delta)
        {
            Decal.AlbedoMix = Mathf.MoveToward(
                Decal.AlbedoMix,
                0,
                FadeSpeed * (float)delta
            );
        }
    }
}