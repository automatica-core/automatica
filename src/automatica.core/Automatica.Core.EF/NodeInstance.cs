using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using Automatica.Core.EF.Exceptions;
using Automatica.Core.Model;

namespace Automatica.Core.EF.Models
{
    public enum NodeInstanceState
    {
        New,
        Saved,
        Loaded,
        Initialized,
        InUse,
        OutOfDatapoits,
        UnknownError,
        Unloaded,
        Unknown
    }
    public partial class NodeInstance : TypedObject
    {
        public bool IsRootInstance()
        {
            return This2ParentNodeInstance == null;
        }

        [NotMapped]
        public NodeInstanceState State { get; set; }

        public void SetProperty(string propertyKey, object value)
        {
            var prop = PropertyInstance.SingleOrDefault(a => a.This2PropertyTemplateNavigation.Key == propertyKey);

            if (prop != null)
            {
                prop.Value = value;
            }
        }

        public PropertyInstance GetProperty(NodeInstance instance, string propertyKey)
        {
            foreach (var prop in instance.PropertyInstance)
            {
                if (prop.This2PropertyTemplateNavigation.Key == propertyKey)
                {
                    return prop;
                }
            }
            throw new PropertyNotFoundException(propertyKey);
        }

        public PropertyInstance GetProperty(string propertyKey)
        {
            return GetProperty(this, propertyKey);
        }

        public object GetPropertyValue(string propertyKey, object defaultValue)
        {
            try
            {
                return GetProperty(this, propertyKey).Value;
            }
            catch (PropertyNotFoundException)
            {
                return defaultValue;
            }
        }

        public int GetPropertyValueInt(string property)
        {
            var prop = GetProperty(property);

            if (prop == null)
            {
                throw new ArgumentException(nameof(property));
            }

            return prop.ValueInt ?? default(int);
        }
        public long GetPropertyValueLong(string property)
        {
            var prop = GetProperty(property);

            if (prop == null)
            {
                throw new ArgumentException(nameof(property));
            }

            return Convert.ToInt64(prop.ValueLong ?? default(long), CultureInfo.InvariantCulture);
        }
        public double GetPropertyValueDouble(string property)
        {
            var prop = GetProperty(property);

            if (prop == null)
            {
                throw new ArgumentException(nameof(property));
            }

            return Convert.ToDouble(prop.ValueDouble ?? default(double), CultureInfo.InvariantCulture);
        }
        public string GetPropertyValueString(string property)
        {
            var prop = GetProperty(property);

            if (prop == null)
            {
                throw new ArgumentException(nameof(property));
            }

            return prop.ValueString;
        }
    }
}
