using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class AtlasMenu : Control
    {
        private Tree _table => GetNode<Tree>("%Table");

        private enum Column
        {
            LevelName,
            Gems,
            Fairies,
            PercentComplete
        }

        public override void _Ready()
        {
            CreateColumns();
            Refresh();
        }

        private void CreateColumns()
        {
            _table.Columns = System.Enum.GetValues<Column>().Length;
            _table.SetColumnTitle((int)Column.LevelName, "Level Name");
            _table.SetColumnTitle((int)Column.Gems, "Gems");
            _table.SetColumnTitle((int)Column.Fairies, "Fairies");
            _table.SetColumnTitle((int)Column.PercentComplete, "% Complete");
        }

        public void Refresh()
        {
            _table.Clear();
            _table.CreateItem();    // The table is actually a tree, so it needs a root

            foreach (string mapName in SaveFile.Current.Maps.Keys.OrderBy(name => name))
            {
                AddRow(mapName);
            }
        }

        private void AddRow(string mapName)
        {
            var progress = SaveFile.Current.Maps[mapName];
            var cacheEntry = AtlasCache.Instance.GetEntry(mapName);

            var row = _table.CreateItem();

            row.SetText((int)Column.LevelName, mapName);
            row.SetTextAlignment((int)Column.LevelName, HorizontalAlignment.Left);

            row.SetText((int)Column.Gems, $"{progress.GemsCollected} / {cacheEntry.TotalGemsInLevel}");
            row.SetTextAlignment((int)Column.Gems, HorizontalAlignment.Center);

            row.SetText((int)Column.Fairies, $"{progress.CollectedFairies.Count} / {cacheEntry.TotalFairiesInLevel}");
            row.SetTextAlignment((int)Column.Fairies, HorizontalAlignment.Center);

            string percentComplete = (SaveFile.Current.GetPercentComplete(mapName) * 100)
                .ToString("0");
            row.SetText((int)Column.PercentComplete, $"{percentComplete}%");
            row.SetTextAlignment((int)Column.PercentComplete, HorizontalAlignment.Center);
        }
    }
}