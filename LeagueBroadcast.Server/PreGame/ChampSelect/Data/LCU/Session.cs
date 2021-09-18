using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Server.PreGame.ChampSelect.Data.LCU
{
    public class Session
    {
        [JsonPropertyName("myTeam")]
        public List<Cell> MyTeam { get; set; } = new();
        [JsonPropertyName("theirTeam")]
        public List<Cell> TheirTeam { get; set; } = new();
        [JsonPropertyName("actions")]
        public List<List<Action>> Actions { get; set; } = new();
        [JsonPropertyName("timer")]
        public SessionTimer Timer { get; set; } = new();
    }
}
