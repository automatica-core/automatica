using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Automatica.Core.Base.Serialization
{
    public static class BinarySerializer
    {
        public static byte[] Serialize(object obj)
        {
            if (obj == null)
                return null;
            var bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }
        public static object Deserialize(byte[] obj)
        {
            if (obj == null)
                return null;
            var bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                ms.Write(obj);
                ms.Position = 0;
                return bf.Deserialize(ms);
            }
        }
    }
}
