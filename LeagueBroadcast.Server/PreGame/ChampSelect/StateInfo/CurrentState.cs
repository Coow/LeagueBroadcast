using Server.PreGame.ChampSelect.Data.LCU;
using System.Text.Json.Serialization;

namespace Server.PreGame.ChampSelect.StateInfo
{
    public class CurrentState
    {
        [JsonPropertyName("isChampSelectActive")]
        public bool IsChampSelectActive { get; set; }
        [JsonPropertyName("session")]
        public Session Session { get; set; }

        public CurrentState(bool IsChampSelectActive, Session Session)
        {
            this.IsChampSelectActive = IsChampSelectActive;
            this.Session = Session;
        }
    }
}
