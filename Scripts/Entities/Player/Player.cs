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
        /// The current level's human-friendly name.
        /// Will be stored in the Atlas cache.
        /// </summary>
        /// <returns></returns>
        [Export] public string LevelName;

        /// <summary>
        /// The map we should return to when "exit level" is selected in the
        /// pause menu, or when a vortex is used.
        ///
        /// Set to null to indicate that this map is a home world.
        /// </summary>
        /// <returns></returns>
        [Export(PropertyHint.File)] public string HomeWorldMap;

        [Export] public float FlyInHeight = 10;
        [Export] public float FlyInDistance = 10;
        [Export] public float FlyInDuration = 4;

        public PlayerState CurrentState => (PlayerState)_stateMachine.CurrentState;

        public PlayerCamera Camera => GetNode<PlayerCamera>("%Camera");
        public Node3D CameraFocus => GetNode<Node3D>("%CameraFocus");
        public Node3D CameraFocusRestPos => GetNode<Node3D>("%CameraFocusRestPos");

        public Node3D Model => GetNode<Node3D>("%Model");
        public AnimationPlayer Animator => GetNode<AnimationPlayer>("%Animator");

        public Area3D KickHitbox => GetNode<Area3D>("%KickHitbox");
        public Area3D DiveExtraHitbox => GetNode<Area3D>("%DiveExtraHitbox");
        public Area3D RollExtraHitbox => GetNode<Area3D>("%RollExtraHitbox");

        public LedgeDetector LedgeDetector => GetNode<LedgeDetector>("%LedgeDetector");
        public Node3D LedgeGrabPoint => GetNode<Node3D>("%LedgeGrabPoint");
        public Node3D MinLedgeGrabHeight => GetNode<Node3D>("%MinLedgeGrabHeight");

        public Node3D FairyKissPoint => GetNode<Node3D>("%FairyKissPoint");
        public Node3D FairyKissCamRightPoint => GetNode<Node3D>("%FairyKissCamRightPoint");
        public Node3D FairyKissCamLeftPoint => GetNode<Node3D>("%FairyKissCamLeftPoint");

        public Node3D LeftHandPoint => GetNode<Node3D>("%LeftHandPoint");
        public Node3D RightHandPoint => GetNode<Node3D>("%RightHandPoint");

        /// <summary>
        /// Used to let the player press "jump" slightly before landing and
        /// still have it count.
        /// </summary>
        public float EarlyJumpBufferTimer { get; private set; }

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

        private float _damageCooldownTimer;

        public override void _Ready()
        {
            AddChild(_stateMachine);
            _stateMachine.StateChanging += OnStateChanging;

            base._Ready();

            SignalBus.Instance.LevelReset += Respawn;
            _spawnPoint = GlobalPosition;
            _spawnRotation = GlobalRotation;

            Respawn();

            // Count all the gems/fairies in the level, including the collected
            // ones, and then cache it.
            //
            // Defer doing so until the next frame, because we don't know if
            // all of the gem containers have spawned in yet.
            Callable.From(UpdateAtlasCache).CallDeferred();
        }

        public override void _Input(InputEvent ev)
        {
            if (InputService.JumpJustPressed(ev))
            {
                EarlyJumpBufferTimer = Player.Default.EarlyJumpBufferTime;
            }
        }

        public void Respawn()
        {
            EmitSignal(SignalName.Respawning);

            if (SaveFile.Current.CurrentCheckpoint == null)
            {
                GlobalPosition = _spawnPoint;
                GlobalRotation = _spawnRotation;
            }
            else
            {
                var checkpoint = GetTree().Root
                    .EnumerateDescendantsOfType<Checkpoint>()
                    .First(c => c.IsCurrent);

                GlobalPosition = checkpoint.GlobalPosition;
                GlobalRotation = checkpoint.GlobalRotation;
            }

            Velocity = Vector3.Zero;
            this.ResetPhysicsInterpolation3D();

            CameraFocus.GlobalTransform = CameraFocusRestPos.GlobalTransform;
            CameraFocus.ResetPhysicsInterpolation3D();
            Camera.Reset();

            Animator.Play("RESET", 0);
            Animator.Advance(0);
            ChangeState<PlayerWalkState>();

            EarlyJumpBufferTimer = 0;

            _damageCooldownTimer = 0;
        }

        public void SetVisibleInPortals(bool visible)
        {
            var visuals = this.EnumerateDescendantsOfType<VisualInstance3D>();
            foreach (var v in visuals)
            {
                v.SetLayerMaskValue(RenderLayer.VisibleInPortals, visible);
            }
        }

        /// <summary>
        /// If the player is currently vulnerable, deals 1 damage and changes
        /// to the given state.  The given state should be some sort of damage
        /// animation state.
        ///
        /// If the player is not currently vulnerable, then this method does
        /// nothing.
        ///
        /// Returns true if the player was successfully damaged, or false otherwise
        /// </summary>
        /// <param name="invulnerablePeriod">
        ///     How many seconds of invulnerability the player receives after
        ///     getting hit by this attack.  The timer starts AFTER after the
        ///     damage animation is completed and the player is back in an
        ///     actionable state.
        /// </param>
        /// <typeparam name="TState"></typeparam>
        public bool TryDamage<TState>(float invulnerablePeriod = 0) where TState : PlayerState, new()
        {
            if (_damageCooldownTimer > 0)
                return false;

            if (CurrentState.Invincible)
                return false;

            _damageCooldownTimer = invulnerablePeriod;
            SaveFile.Current.PlayerHealth--;
            ChangeState<TState>();
            return true;
        }

        private void UpdateAtlasCache()
        {
            AtlasCache.Instance.UpdateCache(SaveFile.Current.CurrentMap, GetTree().CurrentScene);
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            Camera.DisableInput = CurrentState.DisableCameraInput;

            if (EarlyJumpBufferTimer > 0)
                EarlyJumpBufferTimer -= delta;

            if (_damageCooldownTimer > 0 && !CurrentState.PauseDamageCooldownTimer)
                _damageCooldownTimer -= delta;

            AdjustCameraFocusPoint(delta);
        }

        private void AdjustCameraFocusPoint(float delta)
        {
            if (CurrentState.UseMario64CameraFocus)
            {
                var groundPos = FindGroundPosition();
                float yFocusGround = groundPos.Y + CameraFocusRestPos.Position.Y;
                float targetYFocus = Mathf.Max(
                    yFocusGround,
                    GlobalPosition.Y
                );

                var focusPos = CameraFocusRestPos.GlobalPosition;
                focusPos.Y = AccelMath.SmoothStepToward(
                    CameraFocus.GlobalPosition.Y,
                    targetYFocus,
                    Player.Default.Gravity,
                    delta,
                    ref _cameraFocusYSpeed
                );
                CameraFocus.GlobalPosition = focusPos;
            }
            else
            {
                CameraFocus.GlobalPosition = CameraFocusRestPos.GlobalPosition;
            }

            // HACK: Keep the camera focus rotated with the player, so recentering
            // works correctly.
            //
            // The camera uses the global rotation of whatever it's following to
            // determine where to go when it recenters.  Its follow target
            // happens to be the camera focus.  The camera focus is top-level
            // (I forget why), so it doesn't rotate when the player rotates.
            // Thus, it always has a global rotation of (0, 0, 0) unless we
            // intervene, so the camera always faces north when it recenters.
            // To avoid this, just manually change the focus's rotation.
            CameraFocus.GlobalRotation = GlobalRotation;
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
    }
}
