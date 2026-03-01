using System;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class SpawnedGlassPane : AnimatableBody3D, IBreakable
    {
        public bool VulnerableToKick => false;
        public float CameraShakeMagnitude => 0.5f;

        [Export] public double LifespanSeconds;
        [Export] public float Gravity = Player.Default.Gravity;

        [ExportCategory("Internal")]
        [Export] public GpuParticles3D ShatterParticles;
        [Export] public AudioStreamPlayer ShatterSound;
        [Export] public MeshInstance3D MeshInstance;
        [Export] public CollisionShape3D CollisionShape;
        [Export] public Area3D FloorDetector;
        [Export] public CollisionShape3D FloorDetectorShape;

        private readonly StateMachine _stateMachine = new();
        private readonly MeshExploder _meshExploder = new();

        public SpawnedGlassPane()
        {
            AddChild(_stateMachine);
            AddChild(_meshExploder);
        }

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += QueueFree;
            _stateMachine.ChangeState<Idle>();
        }

        public void Initialize(MeshInstance3D originalMesh, Shape3D shape)
        {
            MeshInstance.Mesh = originalMesh.Mesh;
            for (int i = 0; i < originalMesh.GetSurfaceOverrideMaterialCount(); i++)
            {
                var material = originalMesh.GetSurfaceOverrideMaterial(i);
                MeshInstance.SetSurfaceOverrideMaterial(i, material);
            }

            CollisionShape.Shape = shape;
            FloorDetectorShape.Shape = shape;
        }

        public void OnBroken()
        {
            _stateMachine.ChangeState<Shattering>();
        }

        private class Idle : State<SpawnedGlassPane>
        {
            private double _timer;
            private Vector3 _velocity;

            public override void OnStateEntered()
            {
                _timer = 0;
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer += delta;

                if (_timer >= Self.LifespanSeconds)
                {
                    Self.QueueFree();
                    return;
                }

                // Ride on top of any conveyor belts we're sitting on top of.
                // Otherwise, fall.
                //
                // If moving from one conveyor belt to another, don't change
                // direction until we have fully exited the previous one.
                Vector3? floorVelocity = FindPlatformVelocity();

                if (floorVelocity.HasValue)
                    _velocity = floorVelocity.Value;
                else
                    _velocity += Vector3.Down * Self.Gravity * (float)delta;

                Self.GlobalPosition += _velocity * (float)delta;
            }

            private Vector3? FindPlatformVelocity()
            {
                var floorsDetected = Self.FloorDetector
                    .GetOverlappingBodies()
                    .OfType<StaticBody3D>();

                if (!floorsDetected.Any())
                    return null;

                bool foundFloorWithCurrentVelocity = false;
                bool foundFloorWithDifferentVelocity = false;
                Vector3 differentVelocity = default;
                foreach (var floor in floorsDetected)
                {
                    if (floor.ConstantLinearVelocity.IsEqualApprox(_velocity))
                    {
                        foundFloorWithCurrentVelocity = true;
                        continue;
                    }

                    // Don't change velocity if there's two or more conveyor belts
                    // competing with each other
                    if (foundFloorWithDifferentVelocity && !differentVelocity.IsEqualApprox(floor.ConstantLinearVelocity))
                        return _velocity;

                    foundFloorWithDifferentVelocity = true;
                    differentVelocity = floor.ConstantLinearVelocity;
                }

                return (foundFloorWithDifferentVelocity && !foundFloorWithCurrentVelocity)
                    ? differentVelocity
                    : _velocity;
            }
        }

        private class Shattering : State<SpawnedGlassPane>
        {
            private const float Duration = 4f / 60;
            private const float EndScale = 4f;
            private const float HitStopDuration = 0.2f;

            public override void OnStateEntered()
            {
                HitStopManager.Instance.StopFor(HitStopDuration);

                Self.MeshInstance.Visible = false;
                Self.CollisionShape.Disabled = true;

                Self._meshExploder.Explode(
                    Self.MeshInstance,
                    GetTree().FindNode<Player>().GlobalPosition,
                    EndScale,
                    Duration
                );

                var player = GetTree().FindNode<Player>();
                Self.ShatterParticles.GlobalPosition = player.GlobalPosition;
                Self.ShatterParticles.GlobalPosition += Vector3.Up;
                Self.ShatterParticles.Emitting = true;

                Self.ShatterSound.Play();
            }

            public override void _PhysicsProcess(double delta)
            {
                if (!Self.ShatterSound.Playing)
                    Self.QueueFree();
            }
        }
    }
}