using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Base.Templates
{
    /// <summary>
    /// Defines if the database action was successfull
    /// </summary>
    public enum CreateTemplateCode
    {
        Created,
        Updated,
        Error
    }

    [AttributeUsage(AttributeTargets.All)]
    public class EnumNameAttribute : Attribute
    {
        public string TranslationKey { get;  }
        public string EnumValue { get; }

        public EnumNameAttribute(string translationKey, string value = "")
        {
            TranslationKey = translationKey;
            EnumValue = value;
        }
    }

    /// <summary>
    /// Property helper for creating <see cref="PropertyTemplate.Meta"/> strings
    /// </summary>
    public static class PropertyHelper
    {
        /// <summary>
        /// Creates a  <see cref="PropertyTemplate.Meta"/> string for enums
        /// </summary>
        /// <param name="enumType">typeof(enum)</param>
        /// <param name="startEnumValue">MinValue of enum</param>
        /// <param name="endEnumValue">MaxValue of enum</param>
        /// <returns>The meta string</returns>
        public static string CreateEnumMetaString(Type enumType, int? startEnumValue, int? endEnumValue)
        {
            var builder = new StringBuilder("ENUM(");

            var values = Enum.GetValues(enumType);

            foreach (var val in values)
            {
                if (startEnumValue.HasValue && (int)val < startEnumValue.Value)
                {
                    continue;
                }
                if (endEnumValue.HasValue && (int)val > endEnumValue.Value)
                {
                    continue;
                }
                var atts = enumType.GetMember(Enum.GetName(enumType, val))[0].GetCustomAttributes(typeof(EnumNameAttribute), false);
                if (atts.Length > 0 && atts[0] is EnumNameAttribute enumName)
                {
                    builder.Append($"{(int)val},{enumName.TranslationKey};");
                }
                else
                {
                    builder.Append($"{(int) val},{Enum.GetName(enumType, val)};");
                }
            }

            builder.Remove(builder.Length - 1, 1);
            builder.Append(")");
            return builder.ToString();
        }

        /// <summary>
        /// Creates a <see cref="PropertyTemplate.Meta"/> for range property
        /// </summary>
        /// <param name="min">Min value</param>
        /// <param name="max">Max value</param>
        /// <returns>The meta string</returns>
        public static string CreateRangeMetaString(int min, int max)
        {
            return $"RANGE({min},{max})";
        }

        /// <summary>
        /// Creates a <see cref="PropertyTemplate.Meta"/> for range property
        /// </summary>
        /// <param name="min">Min value</param>
        /// <param name="max">Max value</param>
        /// <returns>The meta string</returns>
        public static string CreateRangeMetaString(double min, double max)
        {
            return $"RANGE({min.ToString(CultureInfo.InvariantCulture)},{max.ToString(CultureInfo.InvariantCulture)})";
        }

        /// <summary>
        /// Creates a <see cref="PropertyTemplate.Meta"/> for length property
        /// </summary>
        /// <param name="length">Max length value</param>
        /// <returns>The meta string</returns>
        public static string CreateMaxLengthMetaString(int length)
        {
            return $"LENGTH({length})";
        }


        /// <summary>
        /// Creates a <see cref="PropertyTemplate.Meta"/> or <see cref="Setting.Meta"/> for checklist property
        /// </summary>
        /// <param name="keyValues">Key value list of the items</param>
        /// <returns>The meta string</returns>
        public static string CreateMultiSelect(List<KeyValuePair<int, string>> keyValues)
        {
            var strings = keyValues.Select(kvp => $"{kvp.Key}={kvp.Value}");
            return $"MULTI_SELECT({String.Join("|", strings)})";
        }

        /// <summary>
        /// Creates a <see cref="PropertyTemplate.Meta"/> or <see cref="Setting.Meta"/> for checklist property
        /// </summary>
        /// <param name="enumType">Type of the enum</param>
        /// <returns>The meta string</returns>
        public static string CreateMultiSelect(Type enumType)
        {
            var values = Enum.GetValues(enumType);
            var names = Enum.GetNames(enumType);

            var kvpList = new List<KeyValuePair<int, string>>();

            foreach (var value in values)
            {
                var intValue = (int)value;
                kvpList.Add(new KeyValuePair<int, string>(intValue, names[intValue]));
            }

            return CreateMultiSelect(kvpList);
        }

        public static EnumNameAttribute GetNameAttributeFromEnumValue(Enum value)
        {
            var enumType = value.GetType();
            var atts = enumType.GetMember(Enum.GetName(enumType, value))[0].GetCustomAttributes(typeof(EnumNameAttribute), false);
            if (atts.Length > 0 && atts[0] is EnumNameAttribute enumName)
            {
                return enumName;
            }
            return null;
        }

        /// <summary>
        /// Creates a  <see cref="PropertyTemplate.Meta"/> string for enums
        /// </summary>
        /// <param name="enumType">typeof(enum)</param>
        /// <returns>The meta string</returns>
        public static string CreateEnumMetaString(Type enumType)
        {
            return CreateEnumMetaString(enumType, null, null);
        }
    }


    public interface IPropertyTemplateFactory : ISettingsFactory
    {
        /// <summary>
        /// Creates a new <see cref="PropertyTemplate"/>
        /// </summary>
        /// <param name="uid">The unique id of the property</param>
        /// <param name="name">The name printed in the PropertyEditor UI</param>
        /// <param name="description">The description printed in the PropertyEditor UI</param>
        /// <param name="key">A unique key to find the Property when needed</param>
        /// <param name="propertyType"><see cref="PropertyTemplateType"/></param>
        /// <param name="objectRef">Unique id of the references object (eg. <see cref="NodeTemplate"/>,...)/></param>
        /// <param name="group">A group key</param>
        /// <param name="isVisible">Is visibile to user</param>
        /// <param name="isReadonly">Is readonly for user</param>
        /// <param name="meta">
        /// Meta data <see cref="PropertyHelper"/>
        /// For creating enums or range properties
        /// </param>
        /// <param name="defaultValue">The default value will be set on creating the <see cref="PropertyInstance"/></param>
        /// <param name="groupOrder">Group order</param>
        /// <param name="order">Property order</param>
        /// <returns><see cref="CreateTemplateCode"/></returns>
        CreateTemplateCode CreatePropertyTemplate(Guid uid, string name, string description, string key, PropertyTemplateType propertyType,
            Guid objectRef, string group, bool isVisible, bool isReadonly, string meta, object defaultValue,
            int groupOrder, int order);

        /// <summary>
        /// This allows you to define constraints over properties.
        /// For more info see PropertyConstraints in the docu.
        /// </summary>
        /// <param name="constraintId">Unique id for the constraint</param>
        /// <param name="name">Name of the constraint</param>
        /// <param name="descrption">Description of the constraint</param>
        /// <param name="constraintType"><see cref="PropertyConstraint"/></param>
        /// <param name="level"><see cref="PropertyConstraintLevel"/></param>
        /// <param name="propertyTemplate">The unique id of the <see cref="PropertyTemplate"/></param>
        /// <returns><see cref="CreateTemplateCode"/></returns>
        CreateTemplateCode CreatePropertyConstraint(Guid constraintId, string name, string descrption,
            PropertyConstraint constraintType, PropertyConstraintLevel level, Guid propertyTemplate);

        /// <summary>
        /// Set some additional data for the property constraint.
        ///  For more info see PropertyConstraints in the docu.
        /// </summary>
        /// <param name="constraintData">Unique id for the constraint data</param>
        /// <param name="factor">Factor for the constraint data</param>
        /// <param name="offset">Offset for the constraint data</param>
        /// <param name="propertyTemplateConstraint">The unique id of the PropertyConstraint</param>
        /// <param name="propertyKey">The key of the property to be checked.</param>
        /// <param name="conditionType"><see cref="PropertyConstraintConditionType"/></param>
        /// <returns><see cref="CreateTemplateCode"/></returns>
        CreateTemplateCode CreatePropertyConstraintData(Guid constraintData, double factor, double offset,
            Guid propertyTemplateConstraint, string propertyKey, PropertyConstraintConditionType conditionType);
    }
}
