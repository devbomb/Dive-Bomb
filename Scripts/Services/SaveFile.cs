using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Godot;

namespace FastDragon
{
    public partial class SaveFile : Resource
    {
        public static SaveFile Current = new SaveFile();

        public int TotalGemCount = 0;
        public string CurrentMap;

        public HashSet<string> CollectedGems = new HashSet<string>();
        public Dictionary<GemColor, int> UntalliedGems = new Dictionary<GemColor, int>();

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
                    Formatting = Formatting.Indented
                }
            );
        }

        public bool IsGemCollected(string nodePath)
        {
            return CollectedGems.Contains(nodePath);
        }
    }
}