using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class Portal : Node3D
    {
        [Export(PropertyHint.File)] public string SkyboxEnvironment;
        [Export(PropertyHint.File)] public string TargetMap;
        [Export] public string PortalID;

        private Node3D _playerSpawn => GetNode<Node3D>("%PlayerSpawnPoint");

        private PortalSurface _surface => GetNode<PortalSurface>("%PortalSurface");

        public override void _Ready()
        {
            _surface.TargetMap = TargetMap;
            _surface.SetSkybox(SkyboxEnvironment);
        }

        public void PlayExitAnimation()
        {
            var player = GetTree().Root
                .EnumerateDescendantsOfType<Player>()
                .Single();

            // TODO: Actually play a flying-in animation, instead of just
            // warping the player here.

            player.ChangeState<PlayerWalkState>();
            player.GlobalPosition = _playerSpawn.GlobalPosition;
            player.GlobalRotation = _playerSpawn.GlobalRotation;
            player.ResetPhysicsInterpolation();

            player.Camera.OrbitYawRad = -_playerSpawn.GlobalRotation.Y;
            player.Camera.OrbitPitchRad = 0;
        }
    }
}