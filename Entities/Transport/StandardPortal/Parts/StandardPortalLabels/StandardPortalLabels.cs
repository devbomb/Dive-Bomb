using System;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class StandardPortalLabels : Node3D
    {
        [Export] public Portal Portal;

        [ExportCategory("Internal")]
        [Export] public MeshInstance3D NameLabel;
        [Export] public MeshInstance3D FairiesLabel;
        [Export] public MeshInstance3D BossLevelCountLabel;
        [Export] public AnimationPlayer LabelAnimator;

        private bool _generatedLabels = false;
        private int _lastFairyCount;

        public override void _Process(double delta)
        {
            FlipTowardCamera();

            NameLabel.Visible = Portal.IsUnlocked();

            BossLevelCountLabel.Visible =
                !Portal.IsUnlocked() &&
                Portal.Type == Portal.PortalType.Boss;

            UpdateFairiesLabel();
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

                // Name label
                var nameMesh = (TextMesh)NameLabel.Mesh;
                nameMesh.Text = Portal.Text;

                // Fairy count label
                int totalFairies = SaveFileManager.Current.TotalFairyCount;
                _lastFairyCount = totalFairies;

                var fairiesMesh = (TextMesh)FairiesLabel.Mesh;
                fairiesMesh.Text = $"{totalFairies}/{Portal.FairiesRequired}";

                // Boss level count label
                var levelPortals = GetTree()
                    .Root
                    .EnumerateDescendantsOfType<Portal>()
                    .Where(p => p.Type == Portal.PortalType.Level)
                    .ToArray();

                int completedLevels = levelPortals
                    .Count(p => SaveFileManager.Current.LevelExitReached(p.TargetLevel));

                var levelCountMesh = (TextMesh)BossLevelCountLabel.Mesh;
                levelCountMesh.Text = $"{completedLevels}/{levelPortals.Length}";
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

        private void UpdateFairiesLabel()
        {
            int totalFairies = SaveFileManager.Current.TotalFairyCount;

            FairiesLabel.Visible =
                !Portal.IsUnlocked() &&
                Portal.FairiesRequired > 0 &&
                totalFairies < Portal.FairiesRequired;

            if (!_generatedLabels)
                return;

            if (_lastFairyCount != totalFairies)
            {
                _lastFairyCount = totalFairies;

                var textMesh = (TextMesh)FairiesLabel.Mesh;
                textMesh.Text = $"{totalFairies}/{Portal.FairiesRequired}";
            }
        }
    }
}
