using System.Collections.Generic;
using Godot;

namespace FastDragon
{
    public partial class GemGravityGloves : Node3D
    {
        [Export] public Node3D LeftHandPoint;
        [Export] public Node3D RightHandPoint;
        [Export] public Area3D CollectionArea;
        [Export] public SimpleParticles Particles;

        private Queue<Gem> _gemQueue = new Queue<Gem>();
        private StateMachine _stateMachine = new StateMachine(typeof(GravityGlovesState));

        public override void _Ready()
        {
            AddChild(_stateMachine);

            SignalBus.Instance.LevelReset += Reset;
            Reset();
        }

        private void Reset()
        {
            _stateMachine.ChangeState<Idle>();
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

        private abstract partial class GravityGlovesState : State
        {
            protected GemGravityGloves _parent => _stateMachine.GetParent<GemGravityGloves>();
        }

        private partial class Idle : GravityGlovesState
        {
            public override void OnStateEntered()
            {
                _parent.Particles.Emitting = false;
            }

            public override void OnStateExited()
            {
                _parent.Particles.Emitting = true;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                _parent.QueueNearbyGems();

                if (_parent._gemQueue.Count > 0)
                {
                    _parent.TopLevel = true;
                    // TODO: Choose the closest hand point to the gem
                    _parent.GlobalPosition = _parent.LeftHandPoint.GlobalPosition;
                    _parent.ForceUpdateTransform();
                    _parent.ResetPhysicsInterpolation3D();
                    ChangeState<CollectingGem>();
                    return;
                }
            }
        }

        private partial class CollectingGem : GravityGlovesState
        {
            private const float FlyTime = 0.2f;
            private float _timer;
            private Vector3 _startPos;

            public override void OnStateEntered()
            {
                _timer = 0;
                _startPos = _parent.GlobalPosition;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;
                _timer += delta;

                _parent.QueueNearbyGems();

                Gem gem = PeekAtGemQueue();
                if (gem == null)
                {
                    ChangeState<Idle>();
                    return;
                }

                _parent.GlobalPosition = _startPos.Lerp(
                    gem.GlobalPosition,
                    _timer / FlyTime
                );

                if (_timer >= FlyTime)
                {
                    gem.StartHomingIn();
                    _parent._gemQueue.Dequeue();
                    ChangeState<Idle>();
                }
            }

            private Gem PeekAtGemQueue()
            {
                // Skip gems that aren't revealed(EG: because they were collected
                // manually before we could get to them)
                while (_parent._gemQueue.Count > 0)
                {
                    var gem = _parent._gemQueue.Peek();

                    if (!gem.IsRevealed)
                    {
                        _parent._gemQueue.Dequeue();
                        continue;
                    }

                    return gem;
                }

                return null;
            }
        }
    }
}