using System;

namespace P3.Driver.ZWaveAeon
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class ConfigurationValueAttribute : Attribute
    {
        public int Value { get; }
        public object Discriminator { get; }

        public ConfigurationValueAttribute(int value, object discriminator = null)
        {
            Value = value;
            Discriminator = discriminator;
        }

        public override string ToString()
        {
            return $"Value: {Value}, Discriminator: {Discriminator}";
        }
    }

}
