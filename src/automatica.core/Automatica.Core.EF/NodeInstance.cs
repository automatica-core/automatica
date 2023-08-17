using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using Automatica.Core.EF.Exceptions;
using Automatica.Core.Model;

namespace Automatica.Core.EF.Models
{
    public enum NodeInstanceState
    {
        New = 0,
        Saved = 1,
        Loaded = 2,
        Initialized = 3,
        InUse = 4,
        OutOfDataPoints = 5,
        UnknownError = 6,
        Unloaded = 7,
        Unknown = 8,
        Remote = 9
    }
    public partial class NodeInstance : TypedObject
    {
        public bool IsRootInstance()
        {
            return This2ParentNodeInstance == null;
        }

        public string FullName
        {
            get
            {
                var list = new List<string>();
                list.Add(Name);
                GetFullNameRecursive(this, ref list);
                list.Reverse();
                return String.Join("-", list);
            }
        }

        private void GetFullNameRecursive(NodeInstance instance, ref List<string> names)
        {
            if (instance.This2ParentNodeInstanceNavigation == null)
            {
                return;
            }

            names.Add(instance.This2ParentNodeInstanceNavigation.Name);
            GetFullNameRecursive(instance.This2ParentNodeInstanceNavigation, ref names);
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
                if (prop.This2PropertyTemplateNavigation != null && prop.This2PropertyTemplateNavigation.Key == propertyKey)
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

        public bool? GetPropertyValueBool(string property)
        {
            var prop = GetProperty(property);

            if (prop == null)
            {
                throw new ArgumentException(nameof(property));
            }

            return prop.ValueBool;
        }
    }
}
