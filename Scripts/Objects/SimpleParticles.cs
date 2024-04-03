using Godot;

namespace FastDragon
{
    [Tool]
    public partial class SimpleParticles : Node3D
    {
        [Export] public Texture2D Texture
        {
            get => _textureMaterial.AlbedoTexture;
            set => _textureMaterial.AlbedoTexture = value;
        }

        [Export] public ParticleProcessMaterial ProcessMaterial
        {
            get => (ParticleProcessMaterial)_particles.ProcessMaterial;
            set => _particles.ProcessMaterial = value;
        }

        private StandardMaterial3D _textureMaterial => (StandardMaterial3D)_mesh.Material;
        private QuadMesh _mesh => (QuadMesh)_particles.DrawPass1;

        private readonly GpuParticles3D _particles = new GpuParticles3D
        {
            ProcessMaterial = new ParticleProcessMaterial(),
            DrawPass1 = new QuadMesh
            {
                Size = Vector2.One,
                Material = new StandardMaterial3D
                {
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    VertexColorUseAsAlbedo = true,
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                    BillboardMode = BaseMaterial3D.BillboardModeEnum.Particles,
                    BillboardKeepScale = true
                }
            }
        };

        public SimpleParticles()
        {
            AddChild(_particles, @internal: InternalMode.Front);
        }
    }
}