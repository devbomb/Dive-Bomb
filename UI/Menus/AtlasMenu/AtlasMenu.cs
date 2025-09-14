using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class AtlasMenu : Page
    {
        private GridContainer _table => GetNode<GridContainer>("%Table");

        public override void _Ready()
        {
            Refresh();
        }

        public override void OnPageEntered()
        {
            Refresh();
            GetNode<Button>("%BackButton").GrabFocus();
        }

        public void Refresh()
        {
            var nonHeaderItems = _table.EnumerateChildren()
                .Where(c => !c.IsInGroup("HeaderItem"))
                .ToArray();

            foreach (var child in nonHeaderItems)
            {
                _table.RemoveChild(child);
                child.QueueFree();
            }

            foreach (string levelScenePath in SaveFileManager.Current.Levels.Keys.OrderBy(k => k))
            {
                AddRow(levelScenePath);
            }
        }

        private void AddRow(string levelScenePath)
        {
            var progress = SaveFileManager.Current.Levels[levelScenePath].Progress;
            var summary = AtlasCache.Instance.GetEntry(levelScenePath);

            AddLabel(summary.HumanReadableName);
            AddSpacer();

            AddLabel($"{progress.TotalGemsCollected} / {summary.TotalGemsInLevel}");
            AddSpacer();

            AddLabel($"{progress.CollectedFairies.Count} / {summary.TotalFairiesInLevel}");
            AddSpacer();

            string percentComplete = (SaveFileManager.Current.GetPercentComplete(levelScenePath) * 100)
                .ToString("0");

            AddLabel($"{percentComplete}%");
        }

        private void AddLabel(string text)
        {
            _table.AddChild(new Label
            {
                Text = text
            });
        }

        private void AddSpacer()
        {
            _table.AddChild(new Control());
        }
    }
}