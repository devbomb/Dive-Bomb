using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class AtlasMenu : Control
    {
        private Tree _table => GetNode<Tree>("%Table");

        public override void _Ready()
        {
            CreateColumns();
            Refresh();
        }

        private void CreateColumns()
        {
            _table.Columns = 2;
            _table.SetColumnTitle(0, "Level Name");
            _table.SetColumnTitle(1, "Gems");
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
            var row = _table.CreateItem();

            row.SetText(0, mapName);
            row.SetTextAlignment(0, HorizontalAlignment.Left);

            row.SetText(1, $"{progress.GemsCollected} / {progress.TotalGemsInLevel}");
            row.SetTextAlignment(1, HorizontalAlignment.Center);
        }
    }
}