using Godot;

namespace FastDragon
{
    [GlobalClass]
    public partial class PortalSurface : MeshInstance3D
    {
        [Export] public Shader PortalSurfaceShader;

        public SubViewport SubViewport { get; } = new();
        public Camera3D PortalCamera  { get; } = new();

        public void SetSkybox(Environment skyboxEnvironment)
        {
            PortalCamera.Environment = skyboxEnvironment;
        }

        public override void _Ready()
        {
            SubViewport.AddChild(PortalCamera);
            AddChild(SubViewport);

            // Ensure the sub viewport's resolution always matches the main one.
            GetViewport().Connect(Viewport.SignalName.SizeChanged, Callable.From(SyncViewportSize));
            SyncViewportSize();

            // Ensure we update the portal camera's position _after_ the main
            // camera has already updated its own.
            ProcessPriority = int.MaxValue;

            // The main camera sometimes moves while the game is paused.  We
            // need to make sure the portal camera moves when that happens, too,
            // to avoid breaking the illusion.
            ProcessMode = ProcessModeEnum.Always;

            // Only render objects that are on the "visible in portals" layer
            PortalCamera.CullMask = 2;

            // Create the portal surface material
            var material = new ShaderMaterial();
            material.Shader = PortalSurfaceShader;
            material.SetShaderParameter("viewport_texture", SubViewport.GetTexture());
            MaterialOverride = material;
        }

        public override void _Process(double delta)
        {
            SyncCamera();
        }

        private void SyncViewportSize()
        {
            Vector2 sizeFloat = GetViewport().GetTexture().GetSize();
            SubViewport.Size = new Vector2I((int)sizeFloat.X, (int)sizeFloat.Y);
        }

        private void SyncCamera()
        {
            var mainCamera = GetTree().Root.GetCamera3D();
            PortalCamera.Visible = mainCamera != null;

            if (PortalCamera.Visible)
            {
                PortalCamera.GlobalTransform = mainCamera.GlobalTransform;
            }
        }
    }
}