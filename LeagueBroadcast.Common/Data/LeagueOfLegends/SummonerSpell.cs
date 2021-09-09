using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Common.Data.LeagueOfLegends
{
    public class SummonerSpell
    {
        public static HashSet<SummonerSpell> All { get; set; } = new();

        [JsonPropertyName("id")]
        public int ID { get; set; } = -1;

        [JsonPropertyName("key")]
        public int Key => ID;

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("icon")]
        public string Icon => IconPath;

        [JsonPropertyName("iconPath")]
        public string IconPath { get; set; } = "";
    }
}
