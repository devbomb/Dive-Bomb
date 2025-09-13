using Godot;

namespace FastDragon
{
    public partial class GemsInLevelProgress : Node3D
    {
        private MeshLabel3D _label => GetNode<MeshLabel3D>("%Label");

        private int _gemsCollected;
        private int _gemsInLevel;

        public override void _Process(double deltaD)
        {
            DiveBombLevel level = this.GetLevel();
            if (level != null)
            {
                var levelSummary = AtlasCache.Instance.GetEntry(level.SceneFilePath);

                int prevCollected = _gemsCollected;
                int prevInLevel = _gemsInLevel;

                _gemsCollected = level.GetProgress().TotalGemsCollected;
                _gemsInLevel = levelSummary.TotalGemsInLevel;
                if (prevCollected != _gemsCollected || prevInLevel != _gemsInLevel)
                {
                    _label.Text = $"{_gemsCollected}/{_gemsInLevel}";
                }
            }
        }
    }
}