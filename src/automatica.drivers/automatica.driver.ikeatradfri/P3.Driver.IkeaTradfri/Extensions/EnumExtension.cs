using P3.Driver.IkeaTradfri.Models;

namespace P3.Driver.IkeaTradfri.Extensions
{
    public static class EnumExtension
    {
        public static string ValueAsString(this TradfriConstRoot enumerator)
        {
            return ((int)enumerator).ToString();
        }
        public static string ValueAsString(this TradfriConstAttribute enumerator)
        {
            return ((int)enumerator).ToString();
        }
        public static string ValueAsString(this TradfriConstMireds enumerator)
        {
            return ((int)enumerator).ToString();
        }
    }
}
