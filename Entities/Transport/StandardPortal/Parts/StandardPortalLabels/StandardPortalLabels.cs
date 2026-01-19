using System;
using Godot;

namespace FastDragon
{
    public partial class StandardPortalLabels : Node3D
    {
        [Export] public Portal Portal;

        [ExportCategory("Internal")]
        [Export] public MeshInstance3D FrontLabel;
        [Export] public MeshInstance3D BackLabel;
        [Export] public AnimationPlayer LabelAnimator;

        public override void _Process(double delta)
        {
            FrontLabel.Visible = Portal.IsUnlocked();
            BackLabel.Visible = Portal.IsUnlocked();
        }

        public void ShowLabels()
        {
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
            LabelAnimator.Play("Appear");

            // FrontLabel and BackLabel have the same non-unique(but still
            // scene-local) TextMesh assigned to them in the editor, so we only
            // need to modify one of them to update both of them.
            var textMesh = (TextMesh)FrontLabel.Mesh;
            if (textMesh.Text != Portal.Text)
                textMesh.Text = Portal.Text;
        }

        public void HideLabels()
        {
            LabelAnimator.Play("Disappear");
        }
    }
}
