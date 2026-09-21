using System.Collections.Generic;
using System.Diagnostics;
using Godot;

namespace FastDragon
{
    public readonly struct StoryFlag
    {
        public readonly string Id;

        public readonly FlagPermanence Permanence;
        public enum FlagPermanence
        {
            Permanent,
            Temporary,
            Checkpointable,
        }

        private StoryFlag(string id, FlagPermanence permanence)
        {
            Id = id;
            Permanence = permanence;
        }

        public static StoryFlag Permanent(string id)
        {
            return new(id, FlagPermanence.Permanent);
        }

        public static StoryFlag Temporary(string id)
        {
            return new(id, FlagPermanence.Temporary);
        }

        public static StoryFlag Checkpointable(string id)
        {
            return new(id, FlagPermanence.Checkpointable);
        }
    }

    public static class StoryFlagExtensions
    {
        public static bool IsStoryFlagSet(this Node node, StoryFlag flag)
        {
            return flag.Permanence switch
            {
                StoryFlag.FlagPermanence.Permanent => node
                    .GetLevel()
                    .GetProgress()
                    .StoryFlags
                    .Contains(flag.Id),

                StoryFlag.FlagPermanence.Temporary => SaveFileManager
                    .Current
                    .CurrentLevelVisit
                    .StoryFlags
                    .Contains(flag.Id),

                StoryFlag.FlagPermanence.Checkpointable => SaveFileManager
                    .Current
                    .CurrentLevelVisit
                    .IsCheckpointableFlagSet(flag.Id),

                _ => throw new UnreachableException(),
            };
        }

        public static void SetStoryFlag(this Node node, StoryFlag flag)
        {
            switch (flag.Permanence)
            {
                case StoryFlag.FlagPermanence.Permanent:
                {
                    node.GetLevel()
                        .GetProgress()
                        .StoryFlags
                        .Add(flag.Id);
                    break;
                }

                case StoryFlag.FlagPermanence.Temporary:
                {
                    SaveFileManager
                        .Current
                        .CurrentLevelVisit
                        .StoryFlags
                        .Add(flag.Id);
                    break;
                }

                case StoryFlag.FlagPermanence.Checkpointable:
                {
                    SaveFileManager
                        .Current
                        .CurrentLevelVisit
                        .SetCheckpointableFlag(flag.Id);
                    break;
                }

                default: throw new UnreachableException();
            }

            SaveFileManager.Instance.RequestAutosave();
        }
    }
}