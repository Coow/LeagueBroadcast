using System.Text.Json.Serialization;

namespace Server.PreGame.ChampSelect.Data.LCU
{
    public class Cell
    {
        [JsonPropertyName("cellId")]
        public int CellId { get; set; }
        [JsonPropertyName("championId")]
        public int ChampionId { get; set; }
        [JsonPropertyName("summonerId")]
        public int SummonerId { get; set; }
        [JsonPropertyName("spell1Id")]
        public int Spell1Id { get; set; }
        [JsonPropertyName("spell2Id")]
        public int Spell2Id { get; set; }
    }
}
