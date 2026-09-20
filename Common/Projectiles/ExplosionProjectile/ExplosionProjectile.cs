using System.Collections.Generic;
using Godot;

namespace FastDragon
{
    public partial class ExplosionProjectile : Node3D
    {
        [Export] public bool Reusable;
        [Export] public float Radius = 10;

        [Export] public float CameraShakeMagnitude = 1;
        [Export] public float CameraShakeFrequency = 10;
        [Export] public float CameraShakeDuration = 0.5f;

        [ExportCategory("Internal")]
        [Export] public ShapeCast3D Hitbox;
        [Export] public SphereShape3D SphereShape;
        [Export] public MeshInstance3D Model;
        [Export] public GpuParticles3D ExplosionParticles;

        private readonly StateMachine _stateMachine = new();

        public ExplosionProjectile()
        {
            AddChild(_stateMachine);
        }

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += OnLevelReset;
            Reset();
        }

        private void OnLevelReset()
        {
            if (!Reusable)
            {
                QueueFree();
                return;
            }

            Reset();
        }

        private void Reset()
        {
            _stateMachine.ChangeState<Idle>();
        }

        public void Explode()
        {
            _stateMachine.ChangeState<Growing>();

            GetTree().FindNode<Player>()?.Camera?.Shake(
                CameraShakeMagnitude,
                CameraShakeFrequency,
                CameraShakeDuration
            );

            // Damage everything in the blast radius
            foreach (var body in GetOverlappingObjects())
            {
                if (body is Player p)
                {
                    p.TryDamage<PlayerDamageFlipState>(1);
                    continue;
                }

                if (body is IDamageable d)
                {
                    d.TryExplode();
                    continue;
                }
            }
        }

        private IEnumerable<object> GetOverlappingObjects()
        {
            SphereShape.Radius = Radius;
            Hitbox.ForceShapecastUpdate();

            for (int i = 0; i < Hitbox.GetCollisionCount(); i++)
            {
                yield return Hitbox.GetCollider(i);
            }
        }

        private class Idle : State<ExplosionProjectile>
        {
            public override void OnStateEntered()
            {
                Self.Model.Visible = false;
            }

            public override void OnStateExited()
            {
                Self.Model.Visible = true;
            }
        }

        private class Growing : State<ExplosionProjectile>
        {
            private const double Duration = 0.1;
            private const float MinRadius = 0.1f;
            private double _visualTimer;
            private double _timer;

            public override void OnStateEntered()
            {
                _timer = 0;
                _visualTimer = 0;
                Self.Model.Scale = Vector3.One * MinRadius;

                for (int i = 0; i < 36; i++)
                {
                    Self.ExplosionParticles.EmitParticle(
                        default,
                        default,
                        default,
                        default,
                        default
                    );
                }
            }

            public override void _Process(double delta)
            {
                _visualTimer += delta;

                float t = (float)(_visualTimer / Duration);
                t = 1f - Mathf.Pow(t - 1, 2);
                t = Mathf.Clamp(t, 0, 1);

                float radius = Mathf.Lerp(MinRadius, Self.Radius, t);
                Self.Model.Scale = Vector3.One * radius;
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer += delta;
                if (_timer >= Duration)
                    ChangeState<Fading>();
            }
        }

        private class Fading : State<ExplosionProjectile>
        {
            private const double Duration = 1;
            private const float MinRadius = 0.1f;
            private double _visualTimer;
            private double _timer;

            public override void OnStateEntered()
            {
                _timer = 0;
                _visualTimer = 0;
                Self.Model.Scale = Vector3.One * Self.Radius;
                Self.Model.Transparency = 0;
            }

            public override void OnStateExited()
            {
                Self.Model.Transparency = 0;
            }

            public override void _Process(double delta)
            {
                _visualTimer += delta;

                float t = (float)(_visualTimer / Duration);
                t = 1f - Mathf.Pow(t - 1, 2);
                t = Mathf.Clamp(t, 0, 1);

                Self.Model.Transparency = t;
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer += delta;
                if (_timer >= Duration)
                    ChangeState<WaitingForParticles>();
            }
        }

        private class WaitingForParticles : State<ExplosionProjectile>
        {
            private const double Duration = 1;
            private double _timer;

            public override void OnStateEntered()
            {
                _timer = Duration;
                Self.Model.Visible = false;
            }

            public override void OnStateExited()
            {
                Self.Model.Visible = true;
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer -= delta;
                if (_timer <= 0)
                {
                    if (Self.Reusable)
                        ChangeState<Idle>();
                    else
                        Self.QueueFree();
                }
            }
        }
    }
}