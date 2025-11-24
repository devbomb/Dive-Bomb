using System;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class FairyDoor : StaticBody3D
    {
        [Export] public string FairyId;
        [Export] public Node3D Model;
        [Export] public CollisionShape3D CollisionShape;
        [Export] public Area3D SoftlockFailsafeTrigger;

        private FairyJar _fairy;
        private readonly StateMachine _stateMachine = new();

        public FairyDoor()
        {
            AddChild(_stateMachine);
        }

        public override void _Ready()
        {
            Callable.From(() =>
            {
                if (string.IsNullOrEmpty(FairyId))
                    throw new Exception("Fairy door doesn't have a FairyId");

                _fairy = GetTree().Root
                    .EnumerateDescendantsOfType<FairyJar>()
                    .Where(f => !string.IsNullOrEmpty(f.FairyId))
                    .FirstOrDefault();

                if (_fairy == null)
                    throw new Exception($"Could not find a fairy with id {FairyId}");

                SignalBus.Instance.LevelReset += Reset;
                Reset();
            }).CallDeferred();
        }

        private void Reset()
        {
            if (IsFairyFree())
                _stateMachine.ChangeState<Open>();
            else
                _stateMachine.ChangeState<Closed>();
        }

        private bool IsFairyFree()
        {
            return this.GetLevel()
                ?.GetProgress()
                ?.CollectedFairies
                ?.Contains(_fairy.SaveKey) ?? false;
        }

        private class Closed : State<FairyDoor>
        {
            public override void OnStateEntered()
            {
                Self.Model.Visible = true;
                Self.CollisionShape.Disabled = false;
                Self.SoftlockFailsafeTrigger.BodyEntered += OnSoftlockFailsafeTriggerEntered;
            }

            public override void OnStateExited()
            {
                Self.SoftlockFailsafeTrigger.BodyEntered -= OnSoftlockFailsafeTriggerEntered;
            }

            public override void _PhysicsProcess(double delta)
            {
                if (Self.IsFairyFree())
                    ChangeState<Open>();
            }

            private void OnSoftlockFailsafeTriggerEntered(Node3D body)
            {
                ChangeState<Open>();
            }
        }

        private class Open : State<FairyDoor>
        {
            public override void OnStateEntered()
            {
                Self.Model.Visible = false;
                Self.CollisionShape.Disabled = true;
            }
        }
    }
}