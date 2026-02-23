using System;
using System.Collections.Generic;
using Godot;

namespace FastDragon
{
    public partial class BonkForgivenessTester : Node
    {
        [Export] public float FullBonkHeight;
        [Export] public float ForgivenBonkHeight;
        [Export] public float NoBonkHeight;

        public void FullBonkTest() => RollAtHeight(FullBonkHeight);
        public void ForgivenBonkTest() => RollAtHeight(ForgivenBonkHeight);
        public void NoBonkTest() => RollAtHeight(NoBonkHeight);

        public void SetTimeScale(float timeScale)
        {
            Engine.TimeScale = timeScale;
        }

        private void RollAtHeight(float height)
        {
            SignalBus.Instance.EmitLevelReset();

            var player = GetTree().FindNode<Player>();
            var testPos = this.FindNodeByTargetName<NamedMarker3D>("TestPosition");

            // Put the player in position
            player.GlobalRotation = testPos.GlobalRotation;

            var playerPos = player.GlobalPosition;
            playerPos.Y = height;
            player.GlobalPosition = playerPos;

            player.ResetPhysicsInterpolation3D();
            player.ForceUpdateTransform();

            // Start rolling (regardless of if there's ground beneath our feet)
            player.ChangeState<PlayerRollState>();

            // Force the camera to look in a side view
            var cameraPos = this.FindNodeByTargetName<NamedMarker3D>("CameraPos").GlobalTransform;
            player.Camera.StartManhandling(cameraPos);
        }
    }
}