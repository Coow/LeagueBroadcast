using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    //https://stackoverflow.com/a/50128943
    public static class PropertyCache<T>
    {
        private static readonly Lazy<IReadOnlyCollection<PropertyInfo>> publicPropertiesLazy
            = new(() => typeof(T).GetProperties());

        public static IReadOnlyCollection<PropertyInfo> PublicProperties => PropertyCache<T>.publicPropertiesLazy.Value;
    }
}
