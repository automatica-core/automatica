﻿using System;
using System.ComponentModel.DataAnnotations.Schema;
using Automatica.Core.Model;
using Newtonsoft.Json;

namespace Automatica.Core.EF.Models
{
    public partial class RuleInterfaceInstance : TypedObject
    {

        [NotMapped]
        public bool IsLinked { get; set; }

        [NotMapped]
        public object Value
        {
            get
            {
                if (This2RuleInterfaceTemplateNavigation == null)
                {
                    return null;
                }
                switch (This2RuleInterfaceTemplateNavigation.ParameterDataType)
                {
                    case RuleInterfaceParameterDataType.Double:
                        return ValueDouble;
                    case RuleInterfaceParameterDataType.Integer:
                        return ValueInteger;
                    case RuleInterfaceParameterDataType.Bool:
                        return ValueBool;
                    case RuleInterfaceParameterDataType.Text:
                    case RuleInterfaceParameterDataType.ConstantString:
                    case RuleInterfaceParameterDataType.Color:
                    case RuleInterfaceParameterDataType.Enum:
                        return ValueString;
                    case RuleInterfaceParameterDataType.Timer:
                        if (!String.IsNullOrEmpty(ValueString))
                        {
                            try
                            {
                                return JsonConvert.DeserializeObject<TimerPropertyData>(ValueString);
                            }
                            catch
                            {
                                return null;
                            }
                        }

                        return null;
                    case RuleInterfaceParameterDataType.Calendar:
                        if (!String.IsNullOrEmpty(ValueString))
                        {
                            try
                            {
                                return JsonConvert.DeserializeObject<CalendarPropertyData>(ValueString);
                            }
                            catch
                            {
                                return null;
                            }
                        }

                        return null;
                    case RuleInterfaceParameterDataType.Controls:
                        if (!String.IsNullOrEmpty(ValueString))
                        {
                            try
                            {
                                return JsonConvert.DeserializeObject<ControlConfiguration>(ValueString);
                            }
                            catch
                            {
                                return null;
                            }
                        }

                        return null;
                }

                return null;
            }
            set
            {
                if (value == null)
                {
                    return;
                }
                switch (This2RuleInterfaceTemplateNavigation.ParameterDataType)
                {
                    case RuleInterfaceParameterDataType.Bool:
                        ValueBool = Convert.ToBoolean(value);
                        break;
                    case RuleInterfaceParameterDataType.Double:
                        ValueDouble = Convert.ToDouble(value);
                        break;
                    case RuleInterfaceParameterDataType.Integer:
                        ValueInteger = Convert.ToInt64(value);
                        break;
                    case RuleInterfaceParameterDataType.Text:
                    case RuleInterfaceParameterDataType.ConstantString:
                    case RuleInterfaceParameterDataType.Color:
                    case RuleInterfaceParameterDataType.Enum:
                        ValueString = value.ToString();
                        break;
                    case RuleInterfaceParameterDataType.Timer:
                    case RuleInterfaceParameterDataType.Calendar:
                    case RuleInterfaceParameterDataType.Controls:
                        if (value is string strValue)
                        {
                            ValueString = strValue;
                        }
                        else
                        {
                            ValueString = JsonConvert.SerializeObject(value);
                        }

                        break;
                }
            }
        }

        public static RuleInterfaceInstance CreateFromTemplate(RuleInstance ruleInstance,
            RuleInterfaceTemplate ruleInterfaceTemplate)
        {
            var ruleInterface = new RuleInterfaceInstance
            {
                ObjId = Guid.NewGuid(),
                This2RuleInterfaceTemplate = ruleInterfaceTemplate.ObjId,
                This2RuleInterfaceTemplateNavigation = ruleInterfaceTemplate,
                Value = ruleInterfaceTemplate.DefaultValue,
                This2RuleInstance = ruleInstance.ObjId
            };

            return ruleInterface;
        }


    }
}
