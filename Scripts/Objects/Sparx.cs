using System;
using System.Collections.Generic;
using Godot;

namespace FastDragon
{
    public partial class Sparx : Area3D
    {
        public const float FlySpeed = 5;
        [Export] public Node3D Model;

        private Queue<Gem> _gemQueue = new Queue<Gem>();

        private enum State
        {
            Idle,
            CollectingGem
        }
        private State _currentState = State.Idle;

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += Reset;
            Reset();
        }

        private void Reset()
        {
            Model.Position = Vector3.Zero;
            _gemQueue.Clear();
            _currentState = State.Idle;
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            switch (_currentState)
            {
                case State.Idle:
                {
                    Model.Position = Model.Position.MoveToward(
                        Vector3.Zero,
                        FlySpeed * delta
                    );

                    if (_gemQueue.Count > 0)
                        StartCollectingGem();

                    break;
                }

                case State.CollectingGem:
                {
                    Gem gem = _gemQueue.Peek();
                    Model.GlobalPosition = Model.GlobalPosition.MoveToward(
                        gem.GlobalPosition,
                        FlySpeed * delta
                    );

                    if (Model.GlobalPosition.IsEqualApprox(gem.GlobalPosition))
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

        public void OnBodyEntered(Node3D body)
        {
            if (body is Gem g && g.CurrentState == Gem.State.Revealed && !_gemQueue.Contains(g))
            {
                _gemQueue.Enqueue(g);
            }
        }

        private void StartCollectingGem()
        {
            _currentState = State.CollectingGem;

            var pos = Model.GlobalPosition;
            Model.TopLevel = true;
            Model.GlobalPosition = pos;
        }

        private void ReturnToIdle()
        {
            _currentState = State.Idle;

            var pos = Model.GlobalPosition;
            Model.TopLevel = false;
            Model.GlobalPosition = pos;
        }
    }
}