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

        private (Mesh Mesh, StandardMaterial3D Material)? _particleMatCache;

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
            particleMesh.SurfaceSetMaterial(0, GetUvScaledMaterial(mesh.Mesh, 0.25f));

            // Boom
            ShatterSound.Play();
            ExplosionParticles.Restart();
            ExplosionParticles.Emitting = true;
        }

        private StandardMaterial3D GetUvScaledMaterial(Mesh mesh, float scale)
        {
            // Don't regenerate the material if we've already generated it.
            var cache = _particleMatCache;
            if (cache.HasValue && cache.Value.Mesh == mesh)
                return cache.Value.Material;

            var mat = (StandardMaterial3D)mesh.SurfaceGetMaterial(0).Duplicate();
            mat.Uv1Scale = Vector3.One * scale;

            _particleMatCache = (mesh, mat);
            return mat;
        }

        public void Stop()
        {
            ExplosionParticles.Emitting = false;
        }
    }
}