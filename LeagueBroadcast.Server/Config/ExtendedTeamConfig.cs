using Common.Config;
using Server.Data;
using System;
using System.Text.Json.Serialization;
using Utils;
using Utils.Log;

#pragma warning disable CS8618
namespace Server.Config
{
    public class ExtendedTeamConfig : JsonConfig
    {
        #region NonSerialized
        [JsonIgnore]
        public string TeamName = "DefaultTeam";

        [JsonIgnore]
        public override string Name => TeamName;
        [JsonIgnore]
        public override StringVersion CurrentVersion => new(1,0,0);
        #endregion

        #region Serialized

        public TeamInfo Config { get; set; }

        public string IconLocation { get; set; } = "";
        #endregion

        public override void CheckForUpdate()
        {
            if (FileVersion != CurrentVersion)
            {
                $"{Name} update detected".Info();
            }
        }

        public override void RevertToDefault()
        {
            //Cannot revert to default since there is no default team
            throw new InvalidOperationException();
        }
    }
}
