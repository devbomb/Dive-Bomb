using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class BombableWallFX : Node3D
    {
        public const int ParticlesPerCubicMeter = 2;

        [ExportCategory("Internal")]
        [Export] public AudioStreamPlayer ShatterSound;
        [Export] public GpuParticles3D ExplosionParticles;

        public void Play(MeshInstance3D mesh)
        {
            // Adjust the explosion particles to match the size of the wall
            var boxSize = mesh.GetAabb().Size;

            var processMat = (ParticleProcessMaterial)ExplosionParticles.ProcessMaterial;
            processMat.EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box;
            processMat.EmissionBoxExtents = boxSize / 2;
            ExplosionParticles.GlobalPosition = mesh.GlobalPosition;

            float volume = boxSize.X * boxSize.Y * boxSize.Z;
            ExplosionParticles.Amount = Mathf.CeilToInt(ParticlesPerCubicMeter * volume);

            // Make the particle material match that of the wall
            var particleMesh = ExplosionParticles.DrawPass1;
            particleMesh.SurfaceSetMaterial(0, mesh.Mesh.SurfaceGetMaterial(0));

            // Boom
            ShatterSound.Play();
            ExplosionParticles.Restart();
            ExplosionParticles.Emitting = true;
        }

        public void Stop()
        {
            ExplosionParticles.Emitting = false;
        }
    }
}