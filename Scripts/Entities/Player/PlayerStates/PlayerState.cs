using Godot;

namespace FastDragon
{
    public partial class PlayerState : Node
    {
        protected Player _player => GetParent<Player>();
        protected Node3D _model => GetNode<Node3D>("%Model");

        public virtual void OnStateEntered() {}
        public virtual void OnStateExited() {}
    }
}