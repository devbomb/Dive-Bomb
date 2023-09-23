using System;
using Godot;

namespace FastDragon
{
    public partial class InterpolatedCharacterBody3D : CharacterBody3D
    {
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

            _truePos = GlobalPosition;
            _trueRot = GlobalRotation;

            _prevTruePos = _truePos;
            _prevTrueRot = _trueRot;
        }

        public override void _Process(double delta)
        {
            _timer += delta;
            double t = _timer / _physicsDelta;
            if (t > 1)
                t = 1;

            GlobalPosition = _prevTruePos.Lerp(_truePos, (float)t);
            GlobalRotation = _prevTrueRot.LerpEulerRad(_trueRot, (float)t);
        }

        public void ResetPhysicsInterpolation()
        {
            _truePos = GlobalPosition;
            _trueRot = GlobalRotation;

            _prevTruePos = _truePos;
            _prevTrueRot = _trueRot;
        }

        private void OnPhysicsFrameStarted(double delta)
        {
            _physicsDelta = delta;
            _timer -= delta;

            GlobalPosition = _truePos;
            GlobalRotation = _trueRot;
            ForceUpdateTransform();
        }

        private void OnPhysicsFrameEnded(double delta)
        {
            _prevTruePos = _truePos;
            _prevTrueRot = _trueRot;

            _truePos = GlobalPosition;
            _trueRot = GlobalRotation;
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