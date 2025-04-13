using Godot;

namespace FastDragon
{
    public partial class FairiesInLevelProgress : Node3D
    {
        private MeshLabel3D _label => GetNode<MeshLabel3D>("%Label");

        private int _fairiesCollected;
        private int _fairiesInLevel;

        public override void _Process(double deltaD)
        {
            string currentMap = SaveFile.Current.CurrentMap;
            var mapProgress = SaveFile.Current.CurrentMapProgress;
            var atlasEntry = AtlasCache.Instance.GetEntry(currentMap);

            int prevCollected = _fairiesCollected;
            int prevInLevel = _fairiesInLevel;

            _fairiesCollected = mapProgress.FairiesCollected;
            _fairiesInLevel = atlasEntry.TotalFairiesInLevel;
            if (prevCollected != _fairiesCollected || prevInLevel != _fairiesInLevel)
            {
                _label.Text = $"{_fairiesCollected}/{_fairiesInLevel}";
            }
        }
    }
}