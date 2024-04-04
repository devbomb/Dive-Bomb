using System;
using Godot;

namespace FastDragon
{
    public partial class PhysicsInterpolator3D : Node
    {
        public bool AllowInterpolation = true;

        private Node3D _parent => GetParent<Node3D>();

        private Transform3D _prevTruePos;
        private Transform3D _truePos;


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

            _truePos = _parent.Transform;
            _prevTruePos = _truePos;

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

            _parent.Transform = _prevTruePos.InterpolateWith(_truePos, (float)t);
        }

        public void ResetPhysicsInterpolation()
        {
            _truePos = _parent.Transform;
            _prevTruePos = _truePos;
            _timer = 0;
        }

        private void OnPhysicsFrameStarted(double delta)
        {
            if (!AllowInterpolation)
                return;

            _physicsDelta = delta;
            _timer -= delta;

            _parent.Transform = _truePos;
            _parent.ForceUpdateTransform();
        }

        private void OnPhysicsFrameEnded(double delta)
        {
            if (!AllowInterpolation)
                return;

            _prevTruePos = _truePos;
            _truePos = _parent.Transform;
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