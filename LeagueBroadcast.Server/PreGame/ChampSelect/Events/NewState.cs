using Server.Events;
using Server.PreGame.ChampSelect.StateInfo;
using System.Text.Json.Serialization;

namespace Server.PreGame.ChampSelect.Events
{
    public class NewState : LeagueEvent
    {
        [JsonPropertyName("state")]
        public StateData State { get; set; }
        public NewState(StateData State)
        {
            this.EventType = "newState";
            this.State = State;
        }
    }
}
