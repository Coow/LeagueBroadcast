using Common.Data.LeagueOfLegends;
using Common.JsonConverters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Utils;
using Utils.Log;

namespace Farsight
{
    public class FarsightDataProvider
    {
        public static HashSet<string> Champions = new();

        public static Offsets GameOffsets = new();
        public static GameObject.Offsets ObjectOffsets = new();

        //Override to turn off memory reading at any point
        public static bool ShouldRun = true;

        public HashSet<string> BlacklistedObjectNames = new()
        {
            "testcube",
            "testcuberender",
            "testcuberender10vision",
            "s5test_wardcorpse",
            "sru_camprespawnmarker",
            "sru_plantrespawnmarker",
            "preseason_turret_shield"
        };
        public List<int> BlacklistedObjects = new();

        private int ObjectManagerLocation = 0;

        public FarsightDataProvider()
        {
        }

        public static async Task Init()
        {
            Champions = Champion.All.Select(c => c.Alias).ToHashSet();
            $"Farsight loaded. Found {Champions.Count} Champ names".Info();
            return;
        }

        public void Connect(Process p)
        {
            if (!ShouldRun)
                return;
            MemoryUtils.Initialize(p);
            ObjectManagerLocation = BitConverter.ToInt32(MemoryUtils.ReadMemory(MemoryUtils.m_baseAddress + GameOffsets.Manager, 4), 0);
        }

        public Snapshot CreateSnapshot()
        {
            Snapshot snap = new();
            if (!MemoryUtils.IsConnected || !ShouldRun)
            {
                "Could not create memory snapshot".Warn();
                return snap;
            }

            ReadObjects(snap);
            ClearMissing(snap);

            return snap;
        }

        public GameObject GetGameObject(int networkID)
        {
            DateTime StartTime = DateTime.Now;
            int maxObjects = 500;
            GameObject obj = new();
            byte[] buff = new byte[500];

            Array.Copy(MemoryUtils.ReadMemory(ObjectManagerLocation, 100), 0, buff, 0, 100);

            Queue<int> toVisit = new();
            HashSet<int> visited = new();
            toVisit.Enqueue(buff.ToInt(GameOffsets.MapRoot));

            int read = 0;
            int child1, child2, child3, node;

            while (read < maxObjects && toVisit.Count > 0)
            {
                node = toVisit.Dequeue();
                if (visited.Contains(node))
                {
                    continue;
                }
                read++;
                visited.Add(node);
                buff.Write(MemoryUtils.ReadMemory(node, 0x30));
                child1 = buff.ToInt(0);
                child2 = buff.ToInt(4);
                child3 = buff.ToInt(8);

                toVisit.Enqueue(child1);
                toVisit.Enqueue(child2);
                toVisit.Enqueue(child3);

                uint netID = buff.ToUInt(GameOffsets.MapNodeNetId);

                if (netID - 0x40000000 > 0x100000)
                    continue;

                int addr = buff.ToInt(GameOffsets.MapNodeObject);
                if (addr == 0)
                    continue;

                if (netID == networkID)
                {
                    obj.LoadFromMemory(addr, ObjectOffsets.EXP + 0x4);
                    break;
                }
            }

            $"Found GameObject in {(DateTime.Now - StartTime).TotalMilliseconds}ms".Debug();
            return obj;
        }


        private void ReadObjects(Snapshot snap)
        {
            int maxObjects = 500;
            int[] pointers = new int[maxObjects];
            byte[] buff = new byte[500];

            Array.Copy(MemoryUtils.ReadMemory(ObjectManagerLocation, 100), 0, buff, 0, 100);

            Queue<int> toVisit = new();
            HashSet<int> visited = new();
            toVisit.Enqueue(buff.ToInt(GameOffsets.MapRoot));

            int objNr = 0;
            int read = 0;
            int child1, child2, child3, node;

            while (read < maxObjects && toVisit.Count > 0)
            {
                node = toVisit.Dequeue();
                if (visited.Contains(node))
                {
                    continue;
                }

                read++;
                visited.Add(node);

                buff.Write(MemoryUtils.ReadMemory(node, 0x30));
                child1 = buff.ToInt(0);
                child2 = buff.ToInt(4);
                child3 = buff.ToInt(8);

                toVisit.Enqueue(child1);
                toVisit.Enqueue(child2);
                toVisit.Enqueue(child3);

                uint netID = buff.ToUInt(GameOffsets.MapNodeNetId);

                if (netID - 0x40000000 > 0x100000)
                    continue;

                int addr = buff.ToInt(GameOffsets.MapNodeObject);
                if (addr == 0)
                    continue;

                pointers[objNr++] = addr;
            }

            for (int i = 0; i < objNr; i++)
            {
                int netID = MemoryUtils.ReadMemory(pointers[i] + ObjectOffsets.NetworkID, 4).ToInt();
                if (BlacklistedObjects.Contains(netID))
                    continue;

                GameObject obj;
                if (!snap.ObjectMap.ContainsKey(netID))
                {
                    obj = new();
                    obj.LoadFromMemory(pointers[i], ObjectOffsets.ItemList + 0x4);
                    snap.ObjectMap.Add(netID, obj);
                }
                else
                {
                    obj = snap.ObjectMap[netID];
                    obj.LoadFromMemory(pointers[i], ObjectOffsets.ItemList + 0x4);

                    if (netID != obj.NetworkID)
                        snap.ObjectMap[obj.NetworkID] = obj;
                }

                if (obj.NetworkID != 0)
                {
                    snap.IndexToNetID[obj.ID] = obj.NetworkID;
                    snap.UpdatedThisFrame.Add(obj.NetworkID);
                    if (obj.Name.Length < 2 || BlacklistedObjectNames.Any(s => s.Equals(obj.Name, StringComparison.OrdinalIgnoreCase)))
                        BlacklistedObjects.Add(obj.NetworkID);
                }

                if (obj.IsChampion())
                {
                    snap.Champions.Add(obj);
                    continue;
                }

                if (obj.Name.Contains("Dragon"))
                {
                    snap.Dragon = obj;
                    continue;
                }
                if (obj.Name.Equals("SRU_Baron"))
                {
                    snap.Baron = obj;
                    continue;
                }
                if (obj.Name.Equals("SRU_RiftHerald"))
                {
                    snap.Herald = obj;
                    continue;
                }
            }
        }

        public List<GameObject> GetGameObjectsByNetworkID(HashSet<int> networkIds)
        {
            int maxObjects = 500;
            List<GameObject> champions = new();
            byte[] buff = new byte[500];

            Array.Copy(MemoryUtils.ReadMemory(ObjectManagerLocation, 100), 0, buff, 0, 100);

            Queue<int> toVisit = new();
            HashSet<int> visited = new();
            toVisit.Enqueue(buff.ToInt(GameOffsets.MapRoot));

            int read = 0;
            int child1, child2, child3, node;

            while (read < maxObjects && toVisit.Count > 0)
            {
                node = toVisit.Dequeue();
                if (visited.Contains(node))
                {
                    continue;
                }
                read++;
                visited.Add(node);
                buff.Write(MemoryUtils.ReadMemory(node, 0x30));
                child1 = buff.ToInt(0);
                child2 = buff.ToInt(4);
                child3 = buff.ToInt(8);

                toVisit.Enqueue(child1);
                toVisit.Enqueue(child2);
                toVisit.Enqueue(child3);

                int netID = buff.ToInt(GameOffsets.MapNodeNetId);

                if ((uint)netID - 0x40000000 > 0x100000)
                    continue;

                int addr = buff.ToInt(GameOffsets.MapNodeObject);
                if (addr == 0)
                    continue;

                if (networkIds.Contains(netID))
                {
                    GameObject toAdd = new();
                    toAdd.LoadFromMemory(addr, ObjectOffsets.ItemList + 0x4);
                    champions.Add(toAdd);
                    if (champions.Count == 10)
                        break;
                }
            }
            return champions;
        }

        private void ClearMissing(Snapshot snap)
        {
            foreach (var s in snap.ObjectMap.Keys.Where(key => !snap.UpdatedThisFrame.Contains(key)).ToList())
            {
                snap.ObjectMap.Remove(s);
            }
        }

        public class Offsets
        {
            [JsonConverter(typeof(HexStringJsonConverter))]
            public int Manager = 0x17239D0;

            [JsonConverter(typeof(HexStringJsonConverter))]
            public int MapCount = 0x2c;

            [JsonConverter(typeof(HexStringJsonConverter))]
            public int MapRoot = 0x28;

            [JsonConverter(typeof(HexStringJsonConverter))]
            public int MapNodeNetId = 0x10;

            [JsonConverter(typeof(HexStringJsonConverter))]
            public int MapNodeObject = 0x14;

            [JsonConverter(typeof(HexStringJsonConverter))]
            public int GameTime = 0x14;
        }
    }
}
