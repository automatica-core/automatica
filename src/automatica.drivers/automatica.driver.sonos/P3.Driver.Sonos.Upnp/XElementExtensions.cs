using System;
using System.Globalization;
using System.Xml.Linq;

namespace P3.Driver.Sonos.Upnp
{
    internal static class XElementExtensions
    {
        public static string GetElementValueSafe(this XElement rootElement, XName name, string defaultValue = null)
        {
            return rootElement.GetElementValueSafe<string>(name, defaultValue);
        }

        public static T GetElementValueSafe<T>(this XElement rootElement, XName name, T defaultValue = default(T))
        {
            if (rootElement == null || name == null)
                return defaultValue;


            T returnValue = defaultValue;

            XElement specifiedElement = rootElement.Element(name);
            if (specifiedElement != null)
            {
                string value = specifiedElement.Value;

                try
                {
                    returnValue = (T)Convert.ChangeType(value, typeof(T), CultureInfo.CurrentCulture);
                }
                catch
                {
                }
            }

            return returnValue;
        }

        public static string GetAttributeValueSafe(this XElement rootElement, XName name, string defaultValue = null)
        {
            return rootElement.GetAttributeValueSafe<string>(name, defaultValue);
        }

        public static T GetAttributeValueSafe<T>(this XElement rootElement, XName name, T defaultValue = default(T))
        {
            if (rootElement == null)
                return defaultValue;

            if (name == null)
                return defaultValue;

            T returnValue = defaultValue;

            XAttribute specifiedAttribute = rootElement.Attribute(name);
            if (specifiedAttribute != null)
            {
                string value = specifiedAttribute.Value;

                try
                {
                    returnValue = (T)Convert.ChangeType(value, typeof(T), CultureInfo.CurrentCulture);
                }
                catch
                {
                }
            }

            return returnValue;
        }
    }

}
