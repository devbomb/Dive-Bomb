using System;
using Godot;

namespace FastDragon
{
    public partial class BonkDecal : Node3D
    {
        public const float FadeSpeed = 0.75f;

        [ExportCategory("Internal")]
        [Export] public Decal Decal;
        [Export] public GpuParticles3D StarParticles;

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += Reset;
            Reset();
        }

        private void Reset()
        {
            Visible = false;
        }

        public void Play(Vector3 collisionPoint, Vector3 wallNormal)
        {
            Visible = true;
            Decal.AlbedoMix = 1;

            GlobalRotation = wallNormal.ForwardToEulerAnglesRad();
            GlobalPosition = collisionPoint + (wallNormal * 0.1f);

            StarParticles.Restart();
            StarParticles.Emitting = true;
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