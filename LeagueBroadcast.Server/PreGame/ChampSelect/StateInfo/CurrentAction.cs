using Server.PreGame.ChampSelect.Data.DTO;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Utils;

namespace Server.PreGame.ChampSelect.StateInfo
{
    public class CurrentAction
    {
        [JsonPropertyName("state")]
        public string State { get; set; } = "";
        [JsonPropertyName("data")]
        public List<PickBan> Data { get; set; } = new();
        [JsonPropertyName("team")]
        public string Team { get; set; } = "";
        [JsonPropertyName("num")]
        public int Num { get; set; } = -1;

        public CurrentAction() { }
        public CurrentAction(CurrentAction toCopy)
        {
            toCopy.CopyProperties(this);
        }

        public override bool Equals(object? obj)
        {
            if (obj is not CurrentAction or null)
            {
                return false;
            }

            CurrentAction second = (CurrentAction)obj;

            return State == second.State && ((State == "none" && second.State == "none") || (Team == second.Team && Num == second.Num));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(State, Data, Team, Num);
        }
    }
}
