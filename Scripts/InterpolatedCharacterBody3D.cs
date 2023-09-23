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

        private float _physicsDelta;

        public override void _Ready()
        {
            var beginSpy = new PhysicsProcessSpy(int.MinValue);
            beginSpy.PhysicsProcessed += OnPhysicsFrameStarted;
            AddChild(beginSpy);

            var endSpy = new PhysicsProcessSpy(int.MaxValue);
            endSpy.PhysicsProcessed += OnPhysicsFrameEnded;
            AddChild(endSpy);


            _truePos = GlobalPosition;
            _prevTruePos = _truePos;

            _trueRot = GlobalRotation;
            _prevTrueRot = _trueRot;
        }

        public override void _Process(double deltaD)
        {
            float delta = (float)deltaD;
            float speed = _truePos.DistanceTo(_prevTruePos) / _physicsDelta;
            float rotSpeed = _trueRot.DistanceTo(_prevTrueRot) / _physicsDelta;

            GlobalPosition = GlobalPosition.MoveToward(
                _truePos,
                speed * delta
            );

            GlobalRotation = GlobalRotation.MoveToward(
                _trueRot,
                rotSpeed * delta
            );
        }

        private void OnPhysicsFrameStarted(double deltaD)
        {
            _physicsDelta = (float)deltaD;
            GlobalPosition = _truePos;
            GlobalRotation = _trueRot;
        }

        private void OnPhysicsFrameEnded(double deltaD)
        {
            _prevTruePos = _truePos;
            _prevTrueRot = _trueRot;

            _truePos = GlobalPosition;
            _trueRot = GlobalRotation;

            GlobalPosition = _prevTruePos;
            GlobalRotation = _prevTrueRot;
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