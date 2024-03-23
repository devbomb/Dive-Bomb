using Godot;
using System.Linq;

namespace FastDragon
{
    public partial class WorldPortal : Area3D
    {
        [Export] public string PortalId;
        [Export] public string TargetPortalId;

        [Export] public float SizeX = 1;
        [Export] public float SizeY = 1;

        private Camera3D _portalCamera => GetNode<Camera3D>("%PortalCamera");
        private MeshInstance3D _portalMaterialHolder => GetNode<MeshInstance3D>("%PortalMaterialHolder");
        private BoxShape3D _boxShape => (BoxShape3D)GetNode<CollisionShape3D>("%CollisionShape3D").Shape;
        private PlaneMesh _plane => (PlaneMesh)GetNode<MeshInstance3D>("%Plane").Mesh;

        private WorldPortal _targetPortal;

        private float _cooldownTimer;

        public override void _Ready()
        {
            BodyEntered += OnBodyEntered;
            _boxShape.Size = new Vector3(SizeX, SizeY, 0.5f);
            _plane.Size = new Vector2(SizeX, SizeY);
        }

        public override void _Process(double deltaD)
        {
            if (_targetPortal == null)
                LinkToTargetPortal();

            UpdateCamera();
        }

        public override void _PhysicsProcess(double deltaD)
        {
            if (_cooldownTimer > 0)
                _cooldownTimer -= (float)deltaD;
        }

        private void LinkToTargetPortal()
        {
            _targetPortal = GetTree().Root
                .EnumerateDescendantsOfType<WorldPortal>()
                .FirstOrDefault(p => p.PortalId == TargetPortalId);

            if (_targetPortal == null)
                throw new System.Exception($"Couldn't find a portal with ID {TargetPortalId}");
        }

        private void OnBodyEntered(Node3D body)
        {
            if (_cooldownTimer > 0)
                return;

            if (!(body is Player player))
                return;

            _targetPortal._cooldownTimer = 1;

            player.GlobalTransform = TeleportTransform(player.GlobalTransform);
            player.Velocity = TeleportVelocity(player.Velocity);
            player.ResetPhysicsInterpolation();

            // Move the camera with the player, so it's seamless.
            // Make sure to update its orbit angles, too.
            player.Camera.GlobalTransform = TeleportTransform(player.Camera.GlobalTransform);
            var oldOrbitAngles = new Vector3(player.Camera.OrbitPitchRad, player.Camera.OrbitYawRad, 0);
            var oldOrbitForward = oldOrbitAngles.EulerAnglesRadToForward();
            var newOrbitForward = TeleportVelocity(oldOrbitForward);
            var newOrbitAngles = newOrbitForward.ForwardToEulerAnglesRad();

            player.Camera.OrbitPitchRad = newOrbitAngles.X;
            player.Camera.OrbitYawRad = newOrbitAngles.Y;
            player.Camera.ResetPhysicsInterpolation();

            // HACK: Temporarily disable auto-rotation, because it likes to bug
            // out after the teleport.
            bool oldVal = player.Camera.AllowAutoRotate;
            player.Camera.AllowAutoRotate = false;
            GetTree().CreateTimer(_targetPortal._cooldownTimer * 0.5f, false, true).Timeout += () =>
            {
                player.Camera.AllowAutoRotate = oldVal;
            };
        }

        private void UpdateCamera()
        {
            var mainCamera = GetTree().Root.GetCamera3D();
            var relativePos = GlobalTransform.AffineInverse() * mainCamera.GlobalTransform;
            relativePos = relativePos.Scaled(new Vector3(-1, 1, -1));

            _portalCamera.GlobalTransform = _targetPortal.GlobalTransform * relativePos;

            // HACK: Reduce the chance of the camera being blocked by a wall
            // on the wrong side of the portal
            _portalCamera.Near = _portalCamera.GlobalPosition.DistanceTo(_targetPortal.GlobalPosition) - 2;
        }

        private Transform3D TeleportTransform(Transform3D globalTransform)
        {
            var relativeTransform = GlobalTransform.AffineInverse() * globalTransform;
            relativeTransform = relativeTransform.Rotated(Vector3.Up, Mathf.DegToRad(180));
            return _targetPortal.GlobalTransform * relativeTransform;
        }

        private Vector3 TeleportVelocity(Vector3 velocity)
        {
            var relativeVel = ToLocalVelocity(velocity);
            relativeVel *= new Vector3(-1, 1, -1);
            return _targetPortal.ToGlobalVelocity(relativeVel);
        }

        private Vector3 ToLocalVelocity(Vector3 velocity)
        {
            var start = ToLocal(Vector3.Zero);
            var end = ToLocal(velocity);
            return end - start;
        }

        private Vector3 ToGlobalVelocity(Vector3 v)
        {
            return ToGlobal(ToLocal(Vector3.Zero) + v);
        }
    }
}