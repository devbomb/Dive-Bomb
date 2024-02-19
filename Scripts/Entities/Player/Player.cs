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

        public bool AllowFlaming => CurrentState.AllowFlaming;
        public bool SpawningGemsHomeIn => CurrentState.SpawningGemsHomeIn;

        public PlayerState CurrentState => (PlayerState)_stateMachine.CurrentState;

        public OrbitCamera Camera => GetNode<OrbitCamera>("%Camera");
        public Node3D CameraFocus => GetNode<Node3D>("%CameraFocus");
        public Node3D CameraFocusRestPos => GetNode<Node3D>("%CameraFocusRestPos");

        public Node3D Model => GetNode<Node3D>("%Model");
        public AnimationPlayer Animator => GetNode<AnimationPlayer>("%Animator");

        public Area3D KickHitbox => GetNode<Area3D>("%KickHitbox");

        public LedgeDetector LedgeDetector => GetNode<LedgeDetector>("%LedgeDetector");
        public Node3D LedgeGrabPoint => GetNode<Node3D>("%LedgeGrabPoint");
        public Node3D MinLedgeGrabHeight => GetNode<Node3D>("%MinLedgeGrabHeight");

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

        public float YawRad
        {
            get => GlobalRotation.Y;
            set
            {
                var rot = GlobalRotation;
                rot.Y = value;
                GlobalRotation = rot;
            }
        }

        public float ModelPitchRad
        {
            get => Model.Rotation.X;
            set
            {
                var rot = Model.Rotation;
                rot.X = value;
                Model.Rotation = rot;
            }
        }

        private readonly StateMachine _stateMachine = new StateMachine(typeof(PlayerState));
        private Vector3 _spawnPoint;
        private Vector3 _spawnRotation;

        public override void _Ready()
        {
            AddChild(_stateMachine);
            _stateMachine.StateChanging += OnStateChanging;

            MakeVisibleInPortals();

            base._Ready();

            SignalBus.Instance.LevelReset += Respawn;
            _spawnPoint = Position;
            _spawnRotation = Rotation;

            Respawn();

            // DEBUG: Print the total amount of gems in this level.
            // Defer doing so until the next frame, because we don't know if
            // all of the gem containers are ready yet.
            Callable.From(PrintGemCount).CallDeferred();
        }

        public void Respawn()
        {
            EmitSignal(SignalName.Respawning);
            Position = _spawnPoint;
            Rotation = _spawnRotation;
            Velocity = Vector3.Zero;
            ResetPhysicsInterpolation();

            CameraFocus.GlobalPosition = CameraFocusRestPos.GlobalPosition;
            Camera.ForceRecenter();

            Animator.Play("RESET", 0);
            Animator.Advance(0);
            ChangeState<PlayerWalkState>();
        }

        /// <summary>
        /// If the player is currently vulnerable, deals 1 damage and changes
        /// to the given state.  The given state should be some sort of damage
        /// animation state.
        ///
        /// If the player is not currently vulnerable, then this method does
        /// nothing.
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        public void TryDamage<TState>() where TState : PlayerState, new()
        {
            if (!CurrentState.Invincible)
            {
                SaveFile.Current.PlayerHealth--;
                ChangeState<TState>();
            }
        }

        private void PrintGemCount()
        {
            var gemCounts = new Dictionary<GemColor, int>();
            int totalTreasure = 0;
            int individualGems = 0;

            var allGems = GetTree().CurrentScene.EnumerateDescendantsOfType<Gem>();
            foreach (var gem in allGems)
            {
                if (!gemCounts.ContainsKey(gem.Value))
                    gemCounts[gem.Value] = 0;

                totalTreasure += (int)gem.Value;
                gemCounts[gem.Value]++;
                individualGems++;
            }
            GD.Print($"There is a total of {totalTreasure} treasure in this level");
            GD.Print($"There are {individualGems} individual gems in this level");
            foreach (var kvp in gemCounts)
            {
                GD.Print($"{kvp.Key}: {kvp.Value}");
            }

        }

        public override void _Process(double deltaD)
        {
            Camera.DisableInput = CurrentState.DisableCameraInput;

            var groundPos = ToLocal(FindGroundPosition());
            float yFocusGround = groundPos.Y + CameraFocusRestPos.Position.Y;
            float targetYFocus = Mathf.Max(
                yFocusGround,
                0
            );

            var focusPos = CameraFocusRestPos.Position;
            focusPos.Y = AccelMath.SmoothStepToward(
                CameraFocus.Position.Y,
                targetYFocus,
                Player.Default.Gravity,
                (float)deltaD,
                ref _cameraFocusYSpeed
            );
            CameraFocus.Position = focusPos;
        }

        private float _cameraFocusYSpeed = 0;

        private Vector3 FindGroundPosition()
        {
            const float maxHeight = 10;
            var collision = MoveAndCollide(
                Vector3.Down * maxHeight,
                testOnly: true,
                recoveryAsCollision: true
            );

            return collision == null
                ? (GlobalPosition + (Vector3.Down * maxHeight))
                : collision.GetPosition();
        }

        public void ChangeState<TState>() where TState : PlayerState, new()
        {
            _stateMachine.ChangeState<TState>();
        }

        private void OnStateChanging(State currentState, State incomingState)
        {
            GD.Print($"Changing state to {incomingState.GetType().Name}");
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
