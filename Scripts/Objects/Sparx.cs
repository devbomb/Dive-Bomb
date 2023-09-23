using System;
using System.Collections.Generic;
using Godot;

namespace FastDragon
{
    public partial class Sparx : Node3D
    {
        public const float FlySpeed = 10;
        public const float RotSpeedDeg = 360;

        public const float MinIdlePauseTime = 0.25f;
        public const float MaxIdlePauseTime = 3f;

        private Node3D _model => GetNode<Node3D>("%Model");

        private Queue<Gem> _gemQueue = new Queue<Gem>();

        private enum State
        {
            Idle,
            CollectingGem
        }
        private State _currentState = State.Idle;

        private Vector3 _idlePosition;
        private float _idleTimer;

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += Reset;
            Reset();
        }

        private void Reset()
        {
            ReturnToIdle();
            _model.Position = _idlePosition;
            _gemQueue.Clear();
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            switch (_currentState)
            {
                case State.Idle:
                {
                    _idleTimer -= delta;
                    if (_idleTimer < 0)
                    {
                        ShuffleIdlePosition();
                    }

                    _model.Position = _model.Position.MoveToward(
                        _idlePosition,
                        FlySpeed * delta
                    );

                    _model.RotationDegrees = _model.RotationDegrees.MoveToward(
                        Vector3.Zero,
                        RotSpeedDeg * delta
                    );

                    if (_gemQueue.Count > 0)
                        StartCollectingGem();

                    break;
                }

                case State.CollectingGem:
                {
                    Gem gem = _gemQueue.Peek();

                    _model.LookAt(gem.GlobalPosition);

                    _model.GlobalPosition = _model.GlobalPosition.MoveToward(
                        gem.GlobalPosition,
                        FlySpeed * delta
                    );

                    if (_model.GlobalPosition.IsEqualApprox(gem.GlobalPosition))
                    {
                        gem.StartHomingIn();
                        _gemQueue.Dequeue();
                    }

                    if (_gemQueue.Count <= 0)
                        ReturnToIdle();

                    break;
                }
            }
        }

        public void OnBodyEnteredCollectionRange(Node3D body)
        {
            if (!(body is Gem gem))
                return;

            if (gem.CurrentState != Gem.State.Revealed)
                return;

            if (_gemQueue.Contains(gem))
                return;

            _gemQueue.Enqueue(gem);
            gem.Sparkle();
        }

        private void StartCollectingGem()
        {
            _currentState = State.CollectingGem;
            ToggleTopLevel(true);
        }

        private void ReturnToIdle()
        {
            _currentState = State.Idle;
            ShuffleIdlePosition();

            ToggleTopLevel(false);
        }

        private void ToggleTopLevel(bool topLevel)
        {
            var pos = _model.GlobalPosition;
            var rot = _model.GlobalRotation;

            _model.TopLevel = topLevel;

            _model.GlobalPosition = pos;
            _model.GlobalRotation = rot;
        }

        private void ShuffleIdlePosition()
        {
            const float cylinderRadius = 1.5f;
            const float cylinderHeight = 1.5f;

            float angleRad = Mathf.DegToRad(GD.Randf() * 360);

            _idlePosition = new Vector3(
                cylinderRadius * Mathf.Cos(angleRad),
                (float)GD.RandRange(-cylinderHeight / 2, cylinderHeight / 2),
                cylinderRadius * Mathf.Sin(angleRad)
            );

            _idleTimer = (float)GD.RandRange(MinIdlePauseTime, MaxIdlePauseTime);
        }
    }
}