using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class GemGravityGloves : Node3D
    {
        [Export] public Node3D LeftHandPoint;
        [Export] public Node3D RightHandPoint;

        private AudioStreamPlayer3D _reachOutSound => GetNode<AudioStreamPlayer3D>("%ReachOutSound");

        private Area3D _collectionRange => GetNode<Area3D>("%CollectionRange");
        private Area3D _decalEnabler => GetNode<Area3D>("%DecalEnabler");
        private Decal _decal => GetNode<Decal>("%Decal");

        private Node3D _grabber => GetNode<Node3D>("%Grabber");
        private SimpleParticles _particles => GetNode<SimpleParticles>("%TrailParticles");
        private TrailRenderer3D _trail => GetNode<TrailRenderer3D>("%Trail");
        private Node3D _model => GetNode<Node3D>("%Model");

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

        public override void _PhysicsProcess(double deltaD)
        {
            bool showDecal = QueueableGemsInArea(_decalEnabler).Any();
            float targetAlpha = showDecal
                ? 1
                : 0;

            var m = _decal.Modulate;
            m.A = Mathf.MoveToward(m.A, targetAlpha, 3 * (float)deltaD);
            _decal.Modulate = m;
        }

        private void QueueNearbyGems()
        {
            foreach (var gem in QueueableGemsInArea(_collectionRange))
            {
                _gemQueue.Enqueue(gem);
                gem.Sparkle();
            }
        }

        private IEnumerable<Gem> QueueableGemsInArea(Area3D detectorArea)
        {
            var areas = detectorArea.GetOverlappingAreas();

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

                yield return gem;
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
                _parent._particles.Emitting = false;
                _parent._model.Visible = false;
                _parent._trail.Active = false;
            }

            public override void OnStateExited()
            {
                _parent._particles.Emitting = true;
                _parent._model.Visible = true;
                _parent._trail.Active = true;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                _parent.QueueNearbyGems();

                var gem = _parent.PeekAtGemQueue();
                if (gem != null)
                {
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
            private const float Duration = 0.1f;
            private float _timer;
            private float _visualTimer;

            public override void OnStateEntered()
            {
                _timer = 0;
                _visualTimer = 0;
                _parent._reachOutSound.Play();
            }

            public override void _Process(double deltaD)
            {
                _visualTimer += (float)deltaD;

                Gem gem = _parent.PeekAtGemQueue();
                if (gem == null)
                    return;

                var startPoint = _parent._startHandPoint.GlobalPosition;

                _parent._grabber.GlobalPosition = startPoint.LerpParabola(
                    gem.GlobalPosition,
                    0.5f,
                    _visualTimer / Duration
                );

                var dir = _parent._grabber.GlobalPosition.DirectionTo(startPoint);
                var up = dir.IsEqualApprox(Vector3.Up)
                    ? Vector3.Forward
                    : Vector3.Up;

                if (_parent._grabber.GlobalPosition.DistanceTo(startPoint) > 0.01f)
                    _parent._grabber.LookAt(startPoint, up);

                _parent.ResetPhysicsInterpolation3D();
                _parent.ForceUpdateTransform();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;
                _parent.QueueNearbyGems();

                Gem gem = _parent.PeekAtGemQueue();
                if (gem == null)
                {
                    ChangeState<Idle>();
                    return;
                }

                if (_timer >= Duration)
                {
                    gem.StartHomingIn();
                    _parent._gemQueue.Dequeue();
                    ChangeState<Returning>();
                }
            }
        }

        private partial class Returning : GravityGlovesState
        {
            private const float Duration = 0.15f;
            private float _timer;
            private float _visualTimer;
            private Vector3 _startPoint;

            public override void OnStateEntered()
            {
                _timer = 0;
                _visualTimer = 0;
                _startPoint = _parent._grabber.GlobalPosition;
            }

            public override void _Process(double deltaD)
            {
                _visualTimer += (float)deltaD;

                var endPoint = _parent._startHandPoint.GlobalPosition;

                _parent._grabber.GlobalPosition = _startPoint.Lerp(
                    endPoint,
                    _visualTimer / Duration
                );

                var dir = _parent._grabber.GlobalPosition.DirectionTo(_startPoint);
                var up = dir.IsEqualApprox(Vector3.Up)
                    ? Vector3.Forward
                    : Vector3.Up;

                if (_parent._grabber.GlobalPosition.DistanceTo(_startPoint) > 0.01f)
                    _parent._grabber.LookAt(_startPoint, up);

                _parent.ResetPhysicsInterpolation3D();
                _parent.ForceUpdateTransform();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;
                _parent.QueueNearbyGems();

                if (_timer >= Duration)
                    ChangeState<Idle>();
            }
        }
    }
}