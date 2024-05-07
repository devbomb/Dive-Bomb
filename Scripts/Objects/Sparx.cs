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

        [ExportGroup("Materials")]
        [Export] public Material GoldMaterial;
        [Export] public Material BlueMaterial;
        [Export] public Material GreenMaterial;

        private Node3D _goldParticles => GetNode<Node3D>("%GoldParticles");
        private Node3D _blueParticles => GetNode<Node3D>("%BlueParticles");

        private Queue<Gem> _gemQueue = new Queue<Gem>();

        private StateMachine _stateMachine = new StateMachine(typeof(SparxState));

        public override void _Ready()
        {
            AddChild(_stateMachine);

            SignalBus.Instance.LevelReset += Reset;
            Reset();
        }

        private void Reset()
        {
            if (SaveFile.Current.PlayerHealth > SparxColor.Gone)
                _stateMachine.ChangeState<Idle>();
            else
                _stateMachine.ChangeState<Gone>();

            UpdateSparxColor();

            Position = Vector3.Zero;
            this.ResetPhysicsInterpolation();

            _gemQueue.Clear();
        }

        public override void _PhysicsProcess(double deltaD)
        {
            UpdateSparxColor();
        }


        private bool DisappearIfLastHealth()
        {
            if (SaveFile.Current.PlayerHealth <= SparxColor.Gone)
            {
                _stateMachine.ChangeState<Gone>();
                return true;
            }

            return false;
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

        private void UpdateSparxColor()
        {
            foreach (var meshInstance in this.EnumerateDescendantsOfType<MeshInstance3D>())
            {
                if (meshInstance.IsInGroup("ReplaceColor"))
                    meshInstance.MaterialOverride = MaterialForSparxColor();
            }

            var sparxColor = SaveFile.Current.PlayerHealth;
            _goldParticles.Visible = sparxColor == SparxColor.Gold;
            _blueParticles.Visible = sparxColor == SparxColor.Blue;
        }

        private Material MaterialForSparxColor()
        {
            switch (SaveFile.Current.PlayerHealth)
            {
                case SparxColor.Gold: return GoldMaterial;
                case SparxColor.Blue: return BlueMaterial;
                case SparxColor.Green: return GreenMaterial;

                // Doesn't matter what material we use when Sparx is gone, since
                // he's invisible anyway.
                case SparxColor.Gone:
                case SparxColor.Dead:
                default: return GreenMaterial;
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

                if (_sparx.DisappearIfLastHealth())
                    return;

                _sparx.QueueNearbyGems();

                if (_sparx._gemQueue.Count > 0)
                {
                    ChangeState<CollectingGem>();
                    return;
                }

                IdleMovement(delta);
            }

            private void IdleMovement(float delta)
            {
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
            private float _speed;

            public override void OnStateEntered()
            {
                ToggleTopLevel(true);
                _speed = FlySpeed;
            }

            public override void OnStateExited()
            {
                ToggleTopLevel(false);
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                if (_sparx.DisappearIfLastHealth())
                    return;

                _sparx.QueueNearbyGems();
                FlyTowardNextGemInQueue(delta);
            }

            private void FlyTowardNextGemInQueue(float delta)
            {
                Gem gem = PeekAtGemQueue();
                if (gem == null)
                {
                    ChangeState<Idle>();
                    return;
                }

                _sparx.LookAt(gem.GlobalPosition);

                _sparx.GlobalPosition = _sparx.GlobalPosition.MoveToward(
                    gem.GlobalPosition,
                    _speed * delta
                );

                if (_sparx.GlobalPosition.IsEqualApprox(gem.GlobalPosition))
                {
                    gem.StartHomingIn();
                    _sparx._gemQueue.Dequeue();

                    // Fly faster if there are more gems to pick up
                    _speed = FlySpeed * 2;
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

        private partial class Gone : SparxState
        {
            public override void OnStateEntered()
            {
                _sparx.Visible = false;
                _sparx._gemQueue.Clear();
            }

            public override void OnStateExited()
            {
                _sparx.Visible = true;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                if (SaveFile.Current.PlayerHealth > SparxColor.Gone)
                {
                    ChangeState<Idle>();
                }
            }
        }

        private abstract partial class SparxState : State
        {
            protected Sparx _sparx => _stateMachine.GetParent<Sparx>();
        }
    }
}