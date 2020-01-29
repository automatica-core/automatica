using System;
using System.Text;
using Newtonsoft.Json;

namespace Automatica.Core.Base.Serialization
{
    internal class SerializedObject
    {
        public Type Type { get; set; }
        public object Value { get; set; }
    }

    public static class BinarySerializer
    {
        public static byte[] Serialize(object obj)
        {
            var serializedObject = new SerializedObject
            {
                Type = obj.GetType(),
                Value = obj
            };

            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(serializedObject));
        }
        public static object Deserialize(byte[] obj)
        {
            var strValue = Encoding.UTF8.GetString(obj);
            var deserialized = JsonConvert.DeserializeObject<SerializedObject>(strValue);
            return deserialized.Value;
        }
    }
}
