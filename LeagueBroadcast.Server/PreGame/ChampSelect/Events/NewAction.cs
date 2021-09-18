using Server.Events;
using Server.PreGame.ChampSelect.StateInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Server.PreGame.ChampSelect.Events
{
    public class NewActionEvent : LeagueEvent
    {
        [JsonPropertyName("action")]
        public CurrentAction Action { get; set; }
        public NewActionEvent(CurrentAction action)
        {
            EventType = "newAction";
            this.Action = action;
        }
    }
}
