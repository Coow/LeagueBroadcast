using Common.Config;
using Common.Config.Files;
using Server.Events;
using System.Text.Json.Serialization;

namespace Server.PreGame.ChampSelect.Events
{
    public class Heartbeat : LeagueEvent
    {
        [JsonPropertyName("config")]
        public RCVolusPickBanConfig Config { get; } = ConfigController.Get<RCVolusPickBanConfig>();
        public Heartbeat()
        {
            this.EventType = "heartbeat";
        }
    }
}
