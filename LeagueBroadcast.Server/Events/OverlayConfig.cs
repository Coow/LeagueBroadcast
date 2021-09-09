using Server.Http;

namespace Server.Events
{
    public abstract class OverlayConfig : LeagueEvent
    {
        public FrontEndType type;

        public OverlayConfig() : base("OverlayConfig") { }
    }
}
