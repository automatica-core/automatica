using System;

namespace Automatica.Core.EF.Exceptions
{
    public class PropertyNotFoundException(string propertyName) : Exception
    {
        public string PropertyName { get; } = propertyName;

        public override string Message => base.Message + $"Property {PropertyName} could not be found";
    }
}
