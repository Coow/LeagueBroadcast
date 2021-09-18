using Server.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.PreGame.ChampSelect.Events
{
    public class ChampSelectEndEvent : LeagueEvent
    {
        public ChampSelectEndEvent()
        {
            EventType = "champSelectEnd";
        }
    }
}
