using Godot;

namespace FastDragon
{
    public partial class PortalSurface : Area3D
    {
        [Export(PropertyHint.File)] public string SkyboxEnvironment;
        [Export(PropertyHint.File)] public string TargetLevel;

        private Camera3D _portalCamera => GetNode<Camera3D>("%PortalCamera");

        private MeshInstance3D _portalMaterialHolder => GetNode<MeshInstance3D>("%PortalMaterialHolder");

        private Player _player;
        private Vector3 _playerTargetRotRad;

        public void SetSkybox(string skyboxEnvironment)
        {
            _portalCamera.Environment = ResourceLoader.Load<Environment>(skyboxEnvironment);
        }

        public override void _Ready()
        {
            if (ResourceLoader.Exists(SkyboxEnvironment))
                _portalCamera.Environment = ResourceLoader.Load<Environment>(SkyboxEnvironment);

            foreach (var mesh in this.EnumerateDescendantsOfType<MeshInstance3D>())
            {
                if (mesh != _portalMaterialHolder)
                    mesh.MaterialOverride = _portalMaterialHolder.MaterialOverride;
            }
        }
    }
}