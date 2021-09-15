using Common.Data.LeagueOfLegends;
using Common.JsonConverters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Utils;

namespace Farsight
{
    public class GameObject
    {
        private byte isChampion = byte.MaxValue;

        public const int buffDeepSize = 0x1000;

        public short ID;
        public int NetworkID;
        public short Team;
        public Vector3 Position;
        public string Name = "";
        public float Mana;
        public float MaxMana;
        public float Health;
        public float MaxHealth;
        public float CurrentGold;
        public float GoldTotal;
        public float EXP;
        public int Level;
        public HashSet<ItemSlot>? Items;

        public void LoadFromMemory(int baseAdr, int buffSize = 0x3600)
        {
            byte[] mem = MemoryUtils.ReadMemory(baseAdr, buffSize);

            ID = mem.ToShort(FarsightDataProvider.ObjectOffsets.ID);
            Team = mem.ToShort(FarsightDataProvider.ObjectOffsets.Team);
            Position = new Vector3(
                mem.ToFloat(FarsightDataProvider.ObjectOffsets.Pos),
                mem.ToFloat(FarsightDataProvider.ObjectOffsets.Pos + 4),
                mem.ToFloat(FarsightDataProvider.ObjectOffsets.Pos + 8)
                );
            Health = BitConverter.ToSingle(mem, FarsightDataProvider.ObjectOffsets.Health);
            MaxHealth = mem.ToFloat(FarsightDataProvider.ObjectOffsets.MaxHealth);
            Mana = mem.ToFloat(FarsightDataProvider.ObjectOffsets.Mana);
            MaxMana = mem.ToFloat(FarsightDataProvider.ObjectOffsets.MaxMana);
            NetworkID = mem.ToInt(FarsightDataProvider.ObjectOffsets.NetworkID);
            Name = MemoryUtils.ReadMemory(MemoryUtils.ReadMemory(baseAdr + FarsightDataProvider.ObjectOffsets.Name, 4).ToInt(), 50).DecodeAscii();
            if (IsChampion())
            {
                CurrentGold = mem.ToFloat(FarsightDataProvider.ObjectOffsets.CurrentGold);
                GoldTotal = mem.ToFloat(FarsightDataProvider.ObjectOffsets.GoldTotal);
                EXP = mem.ToFloat(FarsightDataProvider.ObjectOffsets.EXP);
                Level = ChampLevel.EXPToLevel(EXP);
                Items = new(6);
                byte[] itemListBuff = MemoryUtils.ReadMemory(MemoryUtils.ReadMemory<int>(baseAdr + FarsightDataProvider.ObjectOffsets.ItemList), 100);
                int i = 0;
                foreach(ItemSlot item in Items)
                {
                    item.Slot = i;
                    uint itemPtr = itemListBuff.ToUInt(i * 0x10 + FarsightDataProvider.ObjectOffsets.ItemListItem);
                    if (itemPtr == 0)
                        continue;
                    uint itemInfoPtr = MemoryUtils.ReadMemory<uint>((int)(itemPtr + FarsightDataProvider.ObjectOffsets.ItemInfo));
                    if (itemInfoPtr == 0)
                        continue;
                    item.ID = MemoryUtils.ReadMemory<int>((int)(itemInfoPtr + FarsightDataProvider.ObjectOffsets.ItemInfoId));
                }
            }
        }

        private byte LoadIsChampion()
        {
            if (FarsightDataProvider.Champions.Contains(Name))
                return 1;
            return 0;

        }

        public bool IsChampion()
        {
            if (isChampion == byte.MaxValue)
            {
                isChampion = LoadIsChampion();
            }

            return isChampion is not byte.MaxValue and not 0;
        }


        public class Offsets
        {
            [JsonConverter(typeof(HexStringJsonConverter))]
            public int ID = 0x20;

            [JsonConverter(typeof(HexStringJsonConverter))]
            public int NetworkID = 0xCC;

            [JsonConverter(typeof(HexStringJsonConverter))]
            public int Team = 0x4C;

            [JsonConverter(typeof(HexStringJsonConverter))]
            public int Pos = 0xF0;

            [JsonConverter(typeof(HexStringJsonConverter))]
            public int Mana = 0x298;

            [JsonConverter(typeof(HexStringJsonConverter))]
            public int MaxMana = 0x2A8;

            [JsonConverter(typeof(HexStringJsonConverter))]
            public int Health = 0xD98;

            [JsonConverter(typeof(HexStringJsonConverter))]
            public int MaxHealth = 0xDA8;

            [JsonConverter(typeof(HexStringJsonConverter))]
            public int CurrentGold = 0x1B80;

            [JsonConverter(typeof(HexStringJsonConverter))]
            public int GoldTotal = 0x1B90;

            [JsonConverter(typeof(HexStringJsonConverter))]
            public int EXP = 0x334C;

            [JsonConverter(typeof(HexStringJsonConverter))]
            public int Name = 0x2BBC;

            [JsonConverter(typeof(HexStringJsonConverter))]
            public int ItemList;

            [JsonConverter(typeof(HexStringJsonConverter))]
            public int ItemListItem;

            [JsonConverter(typeof(HexStringJsonConverter))]
            public int ItemInfo;

            [JsonConverter(typeof(HexStringJsonConverter))]
            public int ItemInfoId;
        }
    }
}
