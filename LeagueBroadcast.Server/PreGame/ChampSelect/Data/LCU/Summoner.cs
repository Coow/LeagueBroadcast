using System.Text.Json.Serialization;

namespace Server.PreGame.ChampSelect.Data.LCU
{
    public class Summoner
    {
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = "";
        [JsonPropertyName("summonerId")]
        public int SummonerId { get; set; }
    }
}
