using System;
using Godot;

namespace FastDragon
{
    public partial class StandardPortalLabels : Node3D
    {
        [Export] public Portal Portal;

        [ExportCategory("Internal")]
        [Export] public MeshInstance3D NameLabel;
        [Export] public AnimationPlayer LabelAnimator;

        private bool _generatedLabels = false;

        public override void _Process(double delta)
        {
            NameLabel.Visible = Portal.IsUnlocked();

            FlipTowardCamera();
        }

        public void ShowLabels()
        {
            LabelAnimator.Play("Appear");

            // Generating TextMesh text is relatively CPU-intensive; if every
            // portal were to generate its text all at once at the start of the
            // level, it would cause a noticeable hitch, which would spoil the
            // smooth transition illusion.
            //
            // Therefore, we defer generating the mesh until the player comes
            // within some range of the portal(detected via an Area3D placed in
            // the editor, hence why it looks like nothing calls this method).
            // That ensures at most one portal is generating text on frame 1 of
            // the level, keeping the hitch short.

            if (!_generatedLabels)
            {
                _generatedLabels = true;

                var nameMesh = (TextMesh)NameLabel.Mesh;
                nameMesh.Text = Portal.Text;
            }

        }

        public void HideLabels()
        {
            LabelAnimator.Play("Disappear");
        }

        private void FlipTowardCamera()
        {
            float component = GetTree()
                .Root
                .GetCamera3D()
                .GlobalPosition
                .DirectionTo(GlobalPosition)
                .ComponentAlong(Portal.GlobalForward());

            float sign = Mathf.Sign(component);
            Scale = new Vector3(-sign, 1, -sign);
        }
    }
}
