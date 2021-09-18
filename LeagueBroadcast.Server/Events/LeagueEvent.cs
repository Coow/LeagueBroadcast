using System.Text.Json.Serialization;

namespace Server.Events
{
    public abstract class LeagueEvent
    {
        [JsonPropertyName("eventType")]
        public string EventType {  get; set; }

        public LeagueEvent(string eventType)
        {
            EventType = eventType;
        }

        public LeagueEvent() { }
    }
}
