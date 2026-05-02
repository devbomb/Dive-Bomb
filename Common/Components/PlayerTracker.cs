using Godot;

namespace FastDragon
{
    /// <summary>
    ///     Constantly sets its position to the player's position, if a player
    ///     exists.
    /// </summary>
    [GlobalClass]
    public partial class PlayerTracker : Node3D
    {
        [Export] public bool UsePhysicsProcess = true;

        private Player _player = null;

        public override void _Process(double delta)
        {
            if (!UsePhysicsProcess)
                MoveToPlayer();
        }

        public override void _PhysicsProcess(double delta)
        {
            if (UsePhysicsProcess)
                MoveToPlayer();
        }

        private void MoveToPlayer()
        {
            if (!IsInstanceValid(_player))
            {
                _player = GetTree().FindNode<Player>();

                if (_player == null)
                    return;
            }

            GlobalTransform = _player.GlobalTransform;
        }
    }
}