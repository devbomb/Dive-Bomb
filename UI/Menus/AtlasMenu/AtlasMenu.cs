using System.Linq;
using System.Resources;
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

            foreach (string levelManifestPath in SaveFileManager.Current.Levels.Keys.OrderBy(k => k))
            {
                var level = ResourceLoader.Load<LevelManifest>(levelManifestPath);
                AddRow(level);
            }
        }

        private void AddRow(LevelManifest level)
        {
            var progress = SaveFileManager.Current.GetLevelSaveData(level).Progress;
            var collectables = AtlasCache.Instance.GetEntry(level);

            AddLabel(collectables.HumanReadableName);
            AddSpacer();

            AddLabel($"{progress.TotalGemsCollected} / {collectables.TotalGemsInLevel}");
            AddSpacer();

            AddLabel($"{progress.CollectedFairies.Count} / {collectables.TotalFairiesInLevel}");
            AddSpacer();

            string percentComplete = (SaveFileManager.Current.GetPercentComplete(level) * 100)
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