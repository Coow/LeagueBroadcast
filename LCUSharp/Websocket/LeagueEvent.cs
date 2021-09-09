using System.Text.Json;
using System.Text.Json.Serialization;

namespace LCUSharp.Websocket
{
    /// <summary>
    /// Represents a league client event.
    /// </summary>
    public class LeagueEvent
    {
        /// <summary>
        /// The event's data.
        /// </summary>
        [JsonPropertyName("data")]
        public JsonElement Data { get; set; }

        /// <summary>
        /// The event's type.
        /// </summary>
        [JsonPropertyName("eventType")]
        public string EventType { get; set; }

        /// <summary>
        /// The event's uri.
        /// </summary>
        [JsonPropertyName("uri")]
        public string Uri { get; set; }
    }
}
