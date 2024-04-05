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

        [Export] public BaseMaterial3D.BlendModeEnum BlendMode
        {
            get => _textureMaterial.BlendMode;
            set => _textureMaterial.BlendMode = value;
        }

        [Export] public bool LocalCoords
        {
            get => _particles.LocalCoords;
            set => _particles.LocalCoords = value;
        }

        [Export] public bool Emitting
        {
            get => _particles.Emitting;
            set => _particles.Emitting = value;
        }

        [Export] public int Amount
        {
            get => _particles.Amount;
            set => _particles.Amount = value;
        }

        [Export] public float AmountRatio
        {
            get => _particles.AmountRatio;
            set => _particles.AmountRatio = value;
        }

        [Export] public double Lifetime
        {
            get => _particles.Lifetime;
            set => _particles.Lifetime = value;
        }

        [Export] public bool OneShot
        {
            get => _particles.OneShot;
            set => _particles.OneShot = value;
        }

        [Export] public double Preprocess
        {
            get => _particles.Preprocess;
            set => _particles.Preprocess = value;
        }

        [Export] public double SpeedScale
        {
            get => _particles.SpeedScale;
            set => _particles.SpeedScale = value;
        }

        [Export] public float Explosiveness
        {
            get => _particles.Explosiveness;
            set => _particles.Explosiveness = value;
        }

        [Export]
        public float Randomness
        {
            get => _particles.Randomness;
            set => _particles.Randomness = value;
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
            Interpolate = true,
            FractDelta = true,

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

        public void Restart() => _particles.Restart();
    }
}