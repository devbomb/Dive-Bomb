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

        private Queue<Gem> _gemQueue = new Queue<Gem>();

        private StateMachine _stateMachine = new StateMachine(typeof(SparxState));
        private PhysicsInterpolator3D _interpolator = new PhysicsInterpolator3D();

        public override void _Ready()
        {
            AddChild(_stateMachine);
            AddChild(_interpolator);

            SignalBus.Instance.LevelReset += Reset;
            Reset();
        }

        private void Reset()
        {
            _stateMachine.ChangeState<Idle>();
            Position = Vector3.Zero;
            _interpolator.ResetPhysicsInterpolation();

            _gemQueue.Clear();
        }

        public override void _PhysicsProcess(double deltaD)
        {
            QueueNearbyGems();
        }

        private void QueueNearbyGems()
        {
            var areas = CollectionArea.GetOverlappingAreas();

            foreach (var area in areas)
            {
                if (!(area.FirstAncestor<Gem>() is Gem gem))
                    continue;

                if (area != gem.CollectionArea)
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

                _sparx.Position = _sparx.Position.MoveToward(
                    _idlePosition,
                    FlySpeed * delta
                );

                // If the model is clipping into a wall, push it out.
                _sparx.GlobalPosition = RayCast(
                    _sparx.CollectionArea.GlobalPosition,
                    _sparx.GlobalPosition,
                    out bool _
                );

                _sparx.RotationDegrees = _sparx.RotationDegrees.MoveToward(
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
                        _sparx.CollectionArea.GlobalPosition,
                        _sparx.CollectionArea.GlobalPosition + candidatePosition,
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

                _sparx.LookAt(gem.GlobalPosition);

                _sparx.GlobalPosition = _sparx.GlobalPosition.MoveToward(
                    gem.GlobalPosition,
                    FlySpeed * delta
                );

                if (_sparx.GlobalPosition.IsEqualApprox(gem.GlobalPosition))
                {
                    gem.StartHomingIn();
                    _sparx._gemQueue.Dequeue();
                }
            }

            private void ToggleTopLevel(bool topLevel)
            {
                var pos = _sparx.GlobalPosition;
                var rot = _sparx.GlobalRotation;

                _sparx.TopLevel = topLevel;

                _sparx.GlobalPosition = pos;
                _sparx.GlobalRotation = rot;
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