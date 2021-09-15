using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Farsight
{
    public class Snapshot
    {
        public List<GameObject> Champions = new();
        public List<GameObject> Structures = new();
        public GameObject Dragon = new();
        public GameObject Baron = new();
        public GameObject Herald = new();
        public Dictionary<int, GameObject> ObjectMap = new();
        public Dictionary<short, int> IndexToNetID = new();

        public HashSet<int> UpdatedThisFrame = new();
        public Snapshot()
        {
        }
    }
}
