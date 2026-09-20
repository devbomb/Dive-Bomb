using System.Collections.Generic;
using Godot;

namespace FastDragon
{
    public static class StoryFlagExtensions
    {
        public static StoryFlags GetStoryFlags(this Node node)
        {
            var level = node.GetLevel();
            return new(level);
        }

        public struct StoryFlags(DiveBombLevel _level)
        {
            public bool HasPermanent(string flag)
            {
                return _level
                    .GetProgress()
                    .StoryFlags
                    .Contains(flag);
            }

            public void SetPermanent(string flag)
            {
                _level.GetProgress()
                    .StoryFlags
                    .Add(flag);

                SaveFileManager.Instance.RequestAutosave();
            }

            public bool HasTemporary(string flag)
            {
                return SaveFileManager
                    .Current
                    .CurrentLevelVisit
                    .StoryFlags
                    .Contains(flag);
            }

            public void SetTemporary(string flag)
            {
                SaveFileManager
                    .Current
                    .CurrentLevelVisit
                    .StoryFlags
                    .Add(flag);

                SaveFileManager.Instance.RequestAutosave();
            }
        }
    }
}