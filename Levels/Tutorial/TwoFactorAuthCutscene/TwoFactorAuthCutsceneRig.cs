using Godot;
using System;

namespace FastDragon.Levels.Tutorial
{
    public partial class TwoFactorAuthCutsceneRig : Node
    {
        [Export] public string targetname;

        [ExportGroup("Internal")]
        [Export] public AnimationPlayer Animator;

        public void OnBodyEntered(Node3D body)
        {
            Animator.Play("CalmDown");
        }

        public void OnAuthenticateButtonPressed()
        {
            Animator.Play("Doomed");
        }
    }
}
