using System.Text.Json.Serialization;

namespace Server.PreGame.ChampSelect.Data.DTO
{
    public class Ban : PickBan
    {
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }
    }
}
