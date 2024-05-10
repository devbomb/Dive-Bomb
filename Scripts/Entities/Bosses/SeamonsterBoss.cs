using Godot;

namespace FastDragon
{
    public partial class SeamonsterBoss : CharacterBody3D
    {
        [Export] public Node3D InitialSpawnPoint;
        [Export] public Node3D CameraFixPoint;
        [Export] public Node3D[] SpawnPoints = new Node3D[0];

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += Respawn;

            // Defer respawning to ensure the player is ready, since we'll be
            // hijacking the camera.
            CallDeferred(nameof(Respawn));
        }

        public void Respawn()
        {
            GlobalTransform = InitialSpawnPoint.GlobalTransform;
            this.ResetPhysicsInterpolation();

            // Hijack the camera
            GetTree().FindNode<Player>().Camera.FixPosition(CameraFixPoint.GlobalTransform);
        }
    }
}