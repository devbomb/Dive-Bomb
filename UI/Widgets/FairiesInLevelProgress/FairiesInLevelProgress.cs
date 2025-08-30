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
            DiveBombLevel level = this.GetLevel();
            if (level != null)
            {
                var atlasEntry = AtlasCache.Instance.GetEntry(level.SceneFilePath);

                int prevCollected = _fairiesCollected;
                int prevInLevel = _fairiesInLevel;

                _fairiesCollected = level.GetProgress().FairiesCollected;
                _fairiesInLevel = atlasEntry.TotalFairiesInLevel;
                if (prevCollected != _fairiesCollected || prevInLevel != _fairiesInLevel)
                {
                    _label.Text = $"{_fairiesCollected}/{_fairiesInLevel}";
                }
            }
        }
    }
}