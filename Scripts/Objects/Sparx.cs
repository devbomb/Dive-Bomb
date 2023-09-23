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

                    // If the model is clipping into a wall, push it out.
                    _model.GlobalPosition = RayCast(
                        GlobalPosition,
                        _model.GlobalPosition,
                        out bool _
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

            // Keep picking random positions on the edge of our radius until
            // we find one that doesn't intersect a wall.  But only try a few
            // times before giving up.
            for (int i = 0; i < 5; i++)
            {
                float angleRad = Mathf.DegToRad(GD.Randf() * 360);

                var candidatePosition = new Vector3(
                    cylinderRadius * Mathf.Cos(angleRad),
                    (float)GD.RandRange(-cylinderHeight / 2, cylinderHeight / 2),
                    cylinderRadius * Mathf.Sin(angleRad)
                );

                RayCast(
                    GlobalPosition,
                    GlobalPosition + candidatePosition,
                    out bool hitAnything
                );

                if (!hitAnything)
                {
                    _idlePosition = candidatePosition;
                    break;
                }
            }

            _idleTimer = (float)GD.RandRange(MinIdlePauseTime, MaxIdlePauseTime);
        }

        Vector3 RayCast(Vector3 from, Vector3 to, out bool hitAnything)
        {
            var spaceState = GetWorld3D().DirectSpaceState;
            var query = PhysicsRayQueryParameters3D.Create(from, to);
            var hitDict = spaceState.IntersectRay(query);

            if (hitDict.Count > 0)
            {
                hitAnything = true;
                return (Vector3)hitDict["position"];
            }

            hitAnything = false;
            return to;
        }
    }
}