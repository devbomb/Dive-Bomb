using System;
using Godot;

namespace FastDragon
{
    public partial class MeshExploder : Node3D
    {
        private readonly StateMachine _stateMachine = new();
        private readonly MeshInstance3D _meshInstance = new();

        private Vector3 _velocity;
        private float _endScale;
        private float _startTransparency;
        private double _duration;

        public MeshExploder()
        {
            AddChild(_stateMachine);
            AddChild(_meshInstance);

            TopLevel = true;
        }

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += Reset;
            Reset();
        }

        private void Reset()
        {
            _stateMachine.ChangeState<Idle>();
        }

        public void Explode(
            MeshInstance3D originalMesh,
            Vector3 centerPointGlobal,
            float endScale,
            double duration,
            Vector3? velocity = null
        )
        {
            GlobalPosition = centerPointGlobal;
            Scale = Vector3.One;

            _meshInstance.Mesh = originalMesh.Mesh;
            _meshInstance.GlobalTransform = originalMesh.GlobalTransform;
            _meshInstance.Transparency = originalMesh.Transparency;

            for (int i = 0; i < originalMesh.GetSurfaceOverrideMaterialCount(); i++)
            {
                var material = originalMesh.GetSurfaceOverrideMaterial(i);
                _meshInstance.SetSurfaceOverrideMaterial(i, material);
            }

            _startTransparency = originalMesh.Transparency;

            _endScale = endScale;
            _duration = duration;
            _velocity = velocity ?? Vector3.Zero;
            _stateMachine.ChangeState<Exploding>();
        }

        private class Idle : State<MeshExploder>
        {
            public override void OnStateEntered()
            {
                Self.Visible = false;
            }
        }

        private class Exploding : State<MeshExploder>
        {
            private double _timer;

            public override void OnStateEntered()
            {
                Self.Visible = true;
                _timer = 0;
            }

            public override void _Process(double delta)
            {
                _timer += delta;
                Self.GlobalPosition += Self._velocity * (float)delta;

                float t = (float)(_timer / Self._duration);

                Self.Scale = Vector3.One.Lerp(Vector3.One * Self._endScale, t * t);

                Self._meshInstance.Transparency = Mathf.Lerp(
                    Self._startTransparency,
                    1,
                    Mathf.Min(t * 2, 1)
                );

                if (_timer >= Self._duration)
                    ChangeState<Idle>();
            }
        }
    }
}