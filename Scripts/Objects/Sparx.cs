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

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += Reset;
            Reset();
        }

        private void Reset()
        {
            Model.Position = Vector3.Zero;
            _gemQueue.Clear();
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            if (_gemQueue.TryPeek(out Gem gem))
            {
                Model.GlobalPosition = Model.GlobalPosition.MoveToward(
                    gem.GlobalPosition,
                    FlySpeed * delta
                );

                if (Model.GlobalPosition == gem.GlobalPosition)
                {
                    gem.StartHomingIn();
                    _gemQueue.Dequeue();
                }
            }
            else
            {
                Model.Position = Model.Position.MoveToward(
                    Vector3.Zero,
                    FlySpeed * delta
                );
            }
        }

        public void OnBodyEntered(Node3D body)
        {
            if (body is Gem g && g.CurrentState == Gem.State.Revealed && !_gemQueue.Contains(g))
            {
                _gemQueue.Enqueue(g);
            }
        }
    }
}