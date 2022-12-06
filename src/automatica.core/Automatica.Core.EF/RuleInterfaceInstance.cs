using System;
using System.ComponentModel.DataAnnotations.Schema;
using Automatica.Core.Model;
using Newtonsoft.Json;

namespace Automatica.Core.EF.Models
{
    public partial class RuleInterfaceInstance : TypedObject
    {
        
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
                    case RuleInterfaceParameterDataType.Text:
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
                    case RuleInterfaceParameterDataType.Double:
                        ValueDouble = Convert.ToDouble(value);
                        break;
                    case RuleInterfaceParameterDataType.Integer:
                        ValueInteger = Convert.ToInt64(value);
                        break;
                    case RuleInterfaceParameterDataType.Text:
                        ValueString = value.ToString();
                        break;
                    case RuleInterfaceParameterDataType.Timer:
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


    }
}
