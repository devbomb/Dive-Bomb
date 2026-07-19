using Godot;

namespace FastDragon
{
    public partial class XRayWall : StaticBody3D
    {
        private readonly RayCast3D _raycast = new RayCast3D();
        private Player _player;

        private float _transparency = 0;
        private float _targetTransparency = 0;

        public XRayWall()
        {
            _raycast.Visible = false;
        }

        public override void _Ready()
        {
            AddChild(_raycast);
            _raycast.HitFromInside = true;
            _raycast.ExcludeParent = false;
            _raycast.DebugShapeCustomColor = new Color(1, 0, 0);
        }

        public override void _Process(double deltaD)
        {
            _transparency = Mathf.MoveToward(
                _transparency,
                _targetTransparency,
               1.5f * (float)deltaD
            );

            foreach (var mesh in this.EnumerateDescendantsOfType<MeshInstance3D>())
                mesh.Transparency = _transparency;
        }

        public override void _PhysicsProcess(double delta)
        {
            var camera = GetTree().Root.GetCamera3D();
            _player = _player ?? GetTree().FindNode<Player>();

            _raycast.GlobalPosition = camera.GlobalPosition;
            _raycast.TargetPosition = _player.GlobalPosition - _raycast.GlobalPosition;
            _raycast.ForceUpdateTransform();
            _raycast.ForceRaycastUpdate();

            _targetTransparency = IsBlockingCamera()
                ? 0.75f
                : 0;
        }

        private bool IsBlockingCamera()
        {
            var body = _raycast.GetCollider();

            if (body == null)
                return false;

            if (body != this)
            {
                _raycast.AddException((CollisionObject3D)body);
                return false;
            }

            return true;
        }
    }
}