using Common.Config;
using Common.Config.Files;
using Server.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.PreGame.ChampSelect.Data.DTO
{
    public class Meta
    {
        //RCVolus compat
        public string cdn { get; set; } = "https://ddragon.leagueoflegends.com/cdn";
        public Version version { get; set; } = new Version();
    }
}
