using System;

namespace Automatica.Core.EF.Exceptions
{
    public class PropertyNotFoundException : Exception
    {
        public string PropertyName { get; }

        public PropertyNotFoundException(string propertyName)
        {
            PropertyName = propertyName;
        }
    }
}
