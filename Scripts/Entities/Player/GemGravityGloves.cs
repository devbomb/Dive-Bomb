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
        [Export] public Node3D Model;

        private Node3D _startHandPoint;

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

        private Gem PeekAtGemQueue()
        {
            // Skip gems that aren't revealed(EG: because they were collected
            // manually before we could get to them)
            while (_gemQueue.Count > 0)
            {
                var gem = _gemQueue.Peek();

                if (!gem.IsRevealed)
                {
                    _gemQueue.Dequeue();
                    continue;
                }

                return gem;
            }

            return null;
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
                _parent.Model.Visible = false;
            }

            public override void OnStateExited()
            {
                _parent.Particles.Emitting = true;
                _parent.Model.Visible = true;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                _parent.QueueNearbyGems();

                if (_parent._gemQueue.Count > 0)
                {
                    _parent.TopLevel = true;

                    var gem = _parent.PeekAtGemQueue();
                    _parent._startHandPoint = ClosestHandPoint(gem);
                    ChangeState<CollectingGem>();
                    return;
                }
            }

            private Node3D ClosestHandPoint(Gem gem)
            {
                float leftDist = gem
                    .GlobalPosition
                    .DistanceSquaredTo(_parent.LeftHandPoint.GlobalPosition);

                float rightDist = gem
                    .GlobalPosition
                    .DistanceSquaredTo(_parent.RightHandPoint.GlobalPosition);

                return leftDist < rightDist
                    ? _parent.LeftHandPoint
                    : _parent.RightHandPoint;
            }
        }

        private partial class CollectingGem : GravityGlovesState
        {
            private const float FlyTime = 0.2f;
            private float _timer;
            private float _visualTimer;

            public override void OnStateEntered()
            {
                _timer = 0;
                _visualTimer = 0;
            }

            public override void _Process(double deltaD)
            {
                _visualTimer += (float)deltaD;

                _parent.QueueNearbyGems();

                Gem gem = _parent.PeekAtGemQueue();
                if (gem == null)
                    return;

                var startPoint =  _parent._startHandPoint.GlobalPosition;

                _parent.GlobalPosition = startPoint.Lerp(
                    gem.GlobalPosition,
                    _visualTimer / FlyTime
                );

                _parent.LookAt(startPoint);

                _parent.ResetPhysicsInterpolation3D();
                _parent.ForceUpdateTransform();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;

                Gem gem = _parent.PeekAtGemQueue();
                if (gem == null)
                {
                    ChangeState<Idle>();
                    return;
                }

                if (_timer >= FlyTime)
                {
                    gem.StartHomingIn();
                    _parent._gemQueue.Dequeue();
                    ChangeState<Idle>();
                }
            }
        }
    }
}