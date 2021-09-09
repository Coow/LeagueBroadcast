namespace Server.Events
{
    public abstract class LeagueEvent
    {
        public string eventType {  get; set; }

        public LeagueEvent(string eventType)
        {
            this.eventType = eventType;
        }
    }
}
