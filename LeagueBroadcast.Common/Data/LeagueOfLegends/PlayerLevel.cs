
using Utils.Log;
using System;
using System.Collections.Generic;

namespace Common.Data.LeagueOfLegends
{
    public class PlayerLevel : IComparable
    {
        public static List<PlayerLevel> Levels = new()
        {
            new(1, 0),
            new(2, 280),
            new(3, 660),
            new(4, 1140),
            new(5, 1720),
            new(6, 2400),
            new(7, 3180),
            new(8, 4060),
            new(9, 5040),
            new(10, 6120),
            new(11, 7300),
            new(12, 8580),
            new(13, 9960),
            new(14, 11440),
            new(15, 13020),
            new(16, 14700),
            new(17, 16480),
            new(18, 18360)
        };

        public static int EXPToLevel(float exp)
        {
            int index = Levels.BinarySearch(new PlayerLevel(0, exp));
            if (index < 0)
            {
                index = ~index - 1;
            }
            if (index >= 0)
            {
                return Levels[index].level;
            }
            "Tried converting negative XP to Level".Warn();
            return -1;
        }

        public float exp;
        public int level;
        public PlayerLevel(int level, float exp)
        {
            this.level = level;
            this.exp = exp;
        }

        public int CompareTo(object? obj)
        {
            if (obj is null || obj.GetType() != typeof(PlayerLevel))
                return int.MaxValue;
            return exp.CompareTo(((PlayerLevel)obj).exp);
        }
    }
}
