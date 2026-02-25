using System;
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
        [Export] public RayCast3D FloorDetector;

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

                Self.FloorDetector.Position = Vector3.Down * Self.MeshInstance.GetAabb().Size.Y / 2;
                Self.FloorDetector.ForceUpdateTransform();
                Self.FloorDetector.ForceRaycastUpdate();

                if (Self.FloorDetector.IsColliding() && Self.FloorDetector.GetCollider() is StaticBody3D floor)
                {
                    _velocity = floor.ConstantLinearVelocity;
                }
                else
                {
                    _velocity += Vector3.Down * Self.Gravity * (float)delta;
                }

                Self.GlobalPosition += _velocity * (float)delta;
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