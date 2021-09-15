using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace Server.Ingame.Data
{
    public class Player
    {
        public Player? LastAttackedBy { get; set; }
        public byte TicksSinceAttacked { get; set; }
        public string Name { get; set; } = "";
        public string PlayerName { get; set; } = "";
        public CircularBuffer<float> HealthHistory { get; set; } = new(10);
        public float DamageDealt { get; set; }
        public int NetworkID { get; set; }
    }
}
