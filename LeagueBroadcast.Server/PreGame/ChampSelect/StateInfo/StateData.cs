using Common.Config;
using Common.Config.Files;
using Common.Data.LeagueOfLegends;
using Server.PreGame.ChampSelect.Data.DTO;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Utils;

namespace Server.PreGame.ChampSelect.StateInfo
{
    public class StateData
    {
        [JsonPropertyName("champSelectActive")]
        public bool ChampSelectActive { get; set; }
        [JsonPropertyName("leagueConnected")]
        public bool LeagueConnected { get; set; }
        [JsonPropertyName("blueTeam")]
        public Team BlueTeam { get; set; } = new();
        [JsonPropertyName("redTeam")]
        public Team RedTeam { get; set; } = new();
        [JsonPropertyName("meta")]
        public Meta Meta { get; set; } = new();
        [JsonPropertyName("timer")]
        public long Timer { get; set; }
        [JsonPropertyName("state")]
        public string State { get; set; } = "PICK 1";
        [JsonPropertyName("config")]
        public RCVolusPickBanConfig Config { get; } = ConfigController.Get<RCVolusPickBanConfig>();

        public StateData()
        {
        }

        public StateData(StateData toCopy)
        {
            toCopy.CopyProperties(this);
        }

        public CurrentAction GetCurrentAction()
        {
            if (BlueTeam.IsActive && RedTeam.IsActive)
            {
                return new CurrentAction() { State = "none" };
            }
            if (!BlueTeam.IsActive && !RedTeam.IsActive)
            {
                return new CurrentAction() { State = "none" };
            }

            Team activeTeam = BlueTeam.IsActive ? BlueTeam : RedTeam;
            string activeTeamName = BlueTeam.IsActive ? "blueTeam" : "redTeam";

            List<Ban> activeBans = activeTeam.Bans.Where(ban => ban.IsActive).ToList();
            List<Pick> activePicks = activeTeam.Picks.Where(pick => pick.IsActive).ToList();

            if (activeBans.Count > 0)
            {
                return new CurrentAction()
                {
                    State = "ban",
                    Data = new List<PickBan>() { activeBans.ElementAt(0) },
                    Team = activeTeamName,
                    Num = activeTeam.Bans.IndexOf(activeBans.ElementAt(0))
                };

            }

            if (activePicks.Count > 0)
            {
                return new CurrentAction()
                {
                    State = "pick",
                    Data = new List<PickBan>() { activePicks.ElementAt(0) },
                    Team = activeTeamName,
                    Num = activeTeam.Picks.IndexOf(activePicks.ElementAt(0))
                };
            }

            return new CurrentAction() { State = "none" };
        }

        public CurrentAction RefreshAction(CurrentAction action)
        {
            if (action.State == "none")
            {
                return action;
            }

            var team = action.Team == "blueTeam" ? BlueTeam : RedTeam;

            action.Data.Clear();
            //Do not refresh if champ select has ended
            if (team.Picks.Count == 0 && team.Bans.Count == 0)
            {
                return action;
            }

            if (action.State == "ban")
            {
                action.Data.Add(team.Bans.ElementAt(action.Num));
            }
            else
            {
                action.Data.Add(team.Picks.ElementAt(action.Num));
            }

            return action;
        }
    }
}
