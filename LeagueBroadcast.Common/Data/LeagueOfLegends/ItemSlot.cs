using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Data.LeagueOfLegends
{
    public class ItemSlot
    {
        public bool IsEmpty { get; set; } = true;
        public int Slot {  get; set; }
        public int ID { get; set; }
        public float Cost { get; set; }
    }
}
