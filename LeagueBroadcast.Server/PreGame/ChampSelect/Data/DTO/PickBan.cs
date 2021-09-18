using Common.Data.LeagueOfLegends;
using System.Text.Json.Serialization;

namespace Server.PreGame.ChampSelect.Data.DTO
{
    public class PickBan
    {
        [JsonPropertyName("champion")]
        public Champion? Champion { get; set; }
    }
}
