using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace Common
{
    public static class Versions
    {
        public static StringVersion CDrag { get; set; } = new(0);

        public static StringVersion Client { get; set; } = new(0);
    }
}
