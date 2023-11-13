using System;
using Godot;

namespace FastDragon
{
    public partial class PhysicsInterpolator3D : Node
    {
        public bool AllowInterpolation = true;

        private Node3D _parent => GetParent<Node3D>();

        private Vector3 _prevTruePos;
        private Vector3 _truePos;

        private Vector3 _prevTrueRot;
        private Vector3 _trueRot;

        private double _physicsDelta = 1;
        private double _timer;

        public override void _Ready()
        {
            var beginSpy = new PhysicsProcessSpy(int.MinValue);
            beginSpy.PhysicsProcessed += OnPhysicsFrameStarted;
            AddChild(beginSpy);

            var endSpy = new PhysicsProcessSpy(int.MaxValue);
            endSpy.PhysicsProcessed += OnPhysicsFrameEnded;
            AddChild(endSpy);

            _truePos = _parent.Position;
            _trueRot = _parent.Rotation;

            _prevTruePos = _truePos;
            _prevTrueRot = _trueRot;

            ProcessPriority = int.MinValue;
        }

        public override void _Process(double delta)
        {
            if (!AllowInterpolation)
                return;

            _timer += delta;
            double t = _timer / _physicsDelta;
            if (t > 1)
                t = 1;

            _parent.Position = _prevTruePos.Lerp(_truePos, (float)t);
            _parent.Rotation = _prevTrueRot.LerpEulerRad(_trueRot, (float)t);
        }

        public void ResetPhysicsInterpolation()
        {
            _truePos = _parent.Position;
            _trueRot = _parent.Rotation;

            _prevTruePos = _truePos;
            _prevTrueRot = _trueRot;
        }

        private void OnPhysicsFrameStarted(double delta)
        {
            if (!AllowInterpolation)
                return;

            _physicsDelta = delta;
            _timer -= delta;

            _parent.Position = _truePos;
            _parent.Rotation = _trueRot;
            _parent.ForceUpdateTransform();
        }

        private void OnPhysicsFrameEnded(double delta)
        {
            if (!AllowInterpolation)
                return;

            _prevTruePos = _truePos;
            _prevTrueRot = _trueRot;

            _truePos = _parent.Position;
            _trueRot = _parent.Rotation;
        }

        private partial class PhysicsProcessSpy : Node
        {
            public event Action<double> PhysicsProcessed;

            public PhysicsProcessSpy(int priority)
            {
                ProcessPhysicsPriority = priority;
            }

            public override void _PhysicsProcess(double delta)
            {
                PhysicsProcessed?.Invoke(delta);
            }
        }
    }
}