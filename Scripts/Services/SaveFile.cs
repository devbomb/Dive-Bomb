using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Godot;

namespace FastDragon
{
    public partial class SaveFile : Resource
    {
        public static SaveFile Current = new SaveFile();

        public SparxColor PlayerHealth = SparxColor.Gold;
        public string CurrentMap;

        public HashSet<string> CollectedGems = new HashSet<string>();
        public Dictionary<GemColor, int> UntalliedGems = new Dictionary<GemColor, int>();

        public Dictionary<string, MapProgress> Maps = new Dictionary<string, MapProgress>();
        public class MapProgress
        {
            public int GemsCollected = 0;

            // Why is this stored in the save file?  Why would it ever change?
            // Well, simply put, it's because we don't know a level's gem count
            // until we actually load the level and count how many gems there
            // are.
            // We can't do that every time the player looks at their atlas,
            // though, because then they'd need to wait for every level in the
            // game to load, including all of its mesh geometry.  All so they
            // can see a stupid number.
            //
            // I _could_ count each level's gems by hand while I'm designing it,
            // and then hardcode that number somewhere, but...that's awful.  No.
            // I won't do it.
            //
            // I _could_ write a build script(or perhaps a custom Godot importer)
            // that automatically counts each level's gems and saves it into a
            // resource file, but...I don't feel like dealing with that.  Maybe
            // some day, but not today.
            //
            // So, as a compromise, we count all the gems in each level when it
            // is first loaded, and then cache that data in the save file.
            // We can get away with this because:
            // * The atlas only shows levels you've visited, to avoid spoilers
            // * We don't display your full-game completion percentage anywhere
            // * This comment exists, reducing the "WTF?!" factor somewhat
            public int TotalGemsInLevel = 0;

            public double PercentComplete()
            {
                return ((double)GemsCollected) / TotalGemsInLevel;
            }
        }

        [JsonIgnore] public int TotalGemCount => Maps.Values.Sum(l => l.GemsCollected);
        [JsonIgnore] public MapProgress CurrentMapProgress => GetMapProgress(CurrentMap);

        public static void Reset()
        {
            Current = new SaveFile();
        }

        public static SaveFile FromJson(string json)
        {
            return JsonConvert.DeserializeObject<SaveFile>(json);
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(
                this,
                new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,

                }
            );
        }

        public MapProgress GetMapProgress(string map)
        {
            if (!Maps.ContainsKey(map))
                Maps.Add(map, new MapProgress());

            return Maps[map];
        }

        public bool IsGemCollected(string nodePath)
        {
            return CollectedGems.Contains(nodePath);
        }
    }
}