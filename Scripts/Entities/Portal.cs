using Godot;

namespace FastDragon
{
    public partial class Portal : Node3D
    {
        [Export(PropertyHint.File)] public string SkyboxEnvironment;
        [Export(PropertyHint.File)] public string TargetMap;

        private PortalSurface _surface => GetNode<PortalSurface>("%PortalSurface");

        public override void _Ready()
        {
            _surface.TargetMap = TargetMap;
            _surface.SetSkybox(SkyboxEnvironment);
        }
    }
}