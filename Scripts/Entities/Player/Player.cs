using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class Player : InterpolatedCharacterBody3D
    {
        [Signal] public delegate void RespawningEventHandler();

        [Export] public float FlyInHeight = 10;
        [Export] public float FlyInDistance = 10;
        [Export] public float FlyInDuration = 2;

        /// <summary>
        ///     Disabling collision shapes infamously can't happen in a
        ///     collision handler, which is inconvenient.
        ///     Here's a shitty workaround.
        /// </summary>
        public bool PretendColliderDisabled {get; set;}

        public bool AllowFlaming => _currentState.AllowFlaming;
        public bool SpawningGemsHomeIn => _currentState.SpawningGemsHomeIn;

        public PlayerState CurrentState => _currentState;

        public OrbitCamera Camera => GetNode<OrbitCamera>("%Camera");
        public Node3D Model => GetNode<Node3D>("%Model");
        public AnimationPlayer Animator => GetNode<AnimationPlayer>("%Animator");

        public float FSpeed
        {
            get => Velocity.Flattened().Length();
            set
            {
                Vector3 vel = this.GlobalForward() * value;
                vel.Y = Velocity.Y;
                Velocity = vel;
            }
        }

        public float VSpeed
        {
            get => Velocity.Y;
            set
            {
                Vector3 vel = Velocity;
                vel.Y = value;
                Velocity = vel;
            }
        }

        private PlayerState _currentState;
        private Vector3 _spawnPoint;
        private Vector3 _spawnRotation;

        public override void _Ready()
        {
            MakeVisibleInPortals();

            base._Ready();

            SignalBus.Instance.LevelReset += Respawn;
            _spawnPoint = Position;
            _spawnRotation = Rotation;

            Respawn();
        }

        public void Respawn()
        {
            EmitSignal(SignalName.Respawning);
            Position = _spawnPoint;
            Rotation = _spawnRotation;
            Velocity = Vector3.Zero;
            ResetPhysicsInterpolation();

            Camera.ForceRecenter();

            Animator.Play("RESET", 0);
            Animator.Advance(0);
            ChangeState<PlayerWalkState>();
        }

        public void ChangeState<TState>() where TState : PlayerState, new()
        {
            GD.Print($"Changing state to {typeof(TState).Name}");
            _currentState?.OnStateExited();

            foreach (var state in States())
            {
                state.ProcessMode = ProcessModeEnum.Disabled;
            }

            _currentState = States().FirstOrDefault(s => s is TState);
            if (_currentState == null)
            {
                _currentState = new TState();
                AddChild(_currentState);
            }

            _currentState.ProcessMode = ProcessModeEnum.Inherit;
            _currentState.OnStateEntered();
        }

        private IEnumerable<PlayerState> States()
        {
            for (int i = 0; i < GetChildCount(); i++)
            {
                var child = GetChild<Node>(i);

                if (child is PlayerState state)
                    yield return state;
            }
        }

        private void MakeVisibleInPortals()
        {
            var visuals = this.EnumerateDescendantsOfType<VisualInstance3D>();
            foreach (var v in visuals)
            {
                v.SetLayerMaskValue(RenderLayer.VisibleInPortals, true);
            }
        }
    }
}
