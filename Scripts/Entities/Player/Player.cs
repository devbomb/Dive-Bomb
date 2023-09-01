using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class Player : CharacterBody3D
    {
        [Signal] public delegate void RespawningEventHandler();

        /// <summary>
        ///     Disabling collision shapes infamously can't happen in a
        ///     collision handler, which is inconvenient.
        ///     Here's a shitty workaround.
        /// </summary>
        public bool PretendColliderDisabled {get; set;}

        public float DefaultGravity = 9.8f;

        public OrbitCamera Camera => GetNode<OrbitCamera>("%Camera");

        private PlayerState _currentState;
        private Vector3 _spawnPoint;
        private Vector3 _spawnRotation;

        public override void _Ready()
        {
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
            Camera.ForceRecenter();

            ChangeState<PlayerWalkState>();
        }

        public void ChangeState<TState>() where TState : PlayerState
        {
            GD.Print($"Changing state to {typeof(TState).Name}");
            _currentState?.OnStateExited();

            foreach (var state in States())
            {
                state.ProcessMode = ProcessModeEnum.Disabled;
            }

            _currentState = States().First(s => s is TState);
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
    }
}

