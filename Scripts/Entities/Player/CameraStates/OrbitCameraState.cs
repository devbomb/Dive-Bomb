using Godot;

namespace FastDragon
{
    public partial class OrbitCameraState : Node
    {
        protected OrbitCamera _camera => GetParent<OrbitCamera>();

        public virtual void OnStateEntered() {}
        public virtual void OnStateExited() {}
    }
}