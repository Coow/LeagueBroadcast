using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Server.PreGame.ChampSelect.Data.DTO
{
    public class Team
    {
        [JsonPropertyName("bans")]
        public List<Ban> Bans { get; set; } = new List<Ban>();
        [JsonPropertyName("picks")]
        public List<Pick> Picks { get; set; } = new();
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is not Team or null)
            {
                return false;
            }
            Team other = (obj as Team)!;
            bool sameBans = !Bans.Except(other.Bans).ToList().Any() && !other.Bans.Except(Bans).ToList().Any();
            bool samePicks = !Picks.Except(other.Picks).ToList().Any() && !other.Picks.Except(Picks).ToList().Any();
            bool sameActivity = IsActive == other.IsActive;
            return sameActivity && samePicks && sameBans;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Bans, Picks, IsActive);
        }
    }
}
