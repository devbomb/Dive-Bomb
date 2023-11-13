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

        [Export] public Area3D CollectionArea;

        private Node3D _model => GetNode<Node3D>("%Model");

        private Queue<Gem> _gemQueue = new Queue<Gem>();

        private StateMachine _stateMachine = new StateMachine(typeof(SparxState));

        public override void _Ready()
        {
            _model.AddChild(new PhysicsInterpolator3D());
            AddChild(_stateMachine);

            SignalBus.Instance.LevelReset += Reset;
            Reset();
        }

        private void Reset()
        {
            _stateMachine.ChangeState<Idle>();
            _model.Position = Vector3.Zero;
            _gemQueue.Clear();
        }

        public override void _PhysicsProcess(double deltaD)
        {
            QueueNearbyGems();
        }

        private void QueueNearbyGems()
        {
            var bodies = CollectionArea.GetOverlappingBodies();

            foreach (var body in bodies)
            {
                if (!(body is Gem gem))
                    continue;

                if (!gem.IsRevealed)
                    continue;

                if (!gem.TouchedGroundOnce)
                    continue;

                if (_gemQueue.Contains(gem))
                    continue;

                _gemQueue.Enqueue(gem);
                gem.Sparkle();
            }
        }

        private partial class Idle : SparxState
        {
            private float _idleTimer = 0;
            private Vector3 _idlePosition;

            private Node3D _model => _sparx._model;

            public override void OnStateEntered()
            {
                _idleTimer = 0;
                ShuffleIdlePosition();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

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
                    _sparx.GlobalPosition,
                    _model.GlobalPosition,
                    out bool _
                );

                _model.RotationDegrees = _model.RotationDegrees.MoveToward(
                    Vector3.Zero,
                    RotSpeedDeg * delta
                );

                if (_sparx._gemQueue.Count > 0)
                    ChangeState<CollectingGem>();
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
                        _sparx.GlobalPosition,
                        _sparx.GlobalPosition + candidatePosition,
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

            private Vector3 RayCast(Vector3 from, Vector3 to, out bool hitAnything)
            {
                var spaceState = _sparx.GetWorld3D().DirectSpaceState;
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

        private partial class CollectingGem : SparxState
        {
            private Node3D _model => _sparx._model;

            public override void OnStateEntered()
            {
                ToggleTopLevel(true);
            }

            public override void OnStateExited()
            {
                ToggleTopLevel(false);
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                Gem gem = PeekAtGemQueue();
                if (gem == null)
                {
                    ChangeState<Idle>();
                    return;
                }

                _model.LookAt(gem.GlobalPosition);

                _model.GlobalPosition = _model.GlobalPosition.MoveToward(
                    gem.GlobalPosition,
                    FlySpeed * delta
                );

                if (_model.GlobalPosition.IsEqualApprox(gem.GlobalPosition))
                {
                    gem.StartHomingIn();
                    _sparx._gemQueue.Dequeue();
                }
            }

            private void ToggleTopLevel(bool topLevel)
            {
                var pos = _model.GlobalPosition;
                var rot = _model.GlobalRotation;

                _model.TopLevel = topLevel;

                _model.GlobalPosition = pos;
                _model.GlobalRotation = rot;
            }

            private Gem PeekAtGemQueue()
            {
                // Skip gems that aren't revealed(EG: because they were collected
                // manually before Sparx could get to them)
                while (_sparx._gemQueue.Count > 0)
                {
                    var gem = _sparx._gemQueue.Peek();

                    if (!gem.IsRevealed)
                    {
                        _sparx._gemQueue.Dequeue();
                        continue;
                    }

                    return gem;
                }

                return null;
            }
        }

        private abstract partial class SparxState : State
        {
            protected Sparx _sparx => _stateMachine.GetParent<Sparx>();
        }
    }
}