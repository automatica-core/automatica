using System;
using System.ComponentModel.DataAnnotations.Schema;
using Automatica.Core.Model;

namespace Automatica.Core.EF.Models
{
    public partial class Setting : TypedObject
    {
        [NotMapped]
        public object Value
        {
            get
            {
                switch ((PropertyTemplateType)Type)
                {
                    case PropertyTemplateType.Text:
                    case PropertyTemplateType.Parity:
                    case PropertyTemplateType.Interface:
                    case PropertyTemplateType.Ip:
                    case PropertyTemplateType.Color:
                        return ValueText;
                    case PropertyTemplateType.Time:
                        return DateTime.Parse(ValueText);
                    case PropertyTemplateType.DropDown:
                    case PropertyTemplateType.Baudrate:
                    case PropertyTemplateType.Databits:
                    case PropertyTemplateType.Integer:
                    case PropertyTemplateType.Enum:
                    case PropertyTemplateType.Range:
                        return ValueInt;
                    case PropertyTemplateType.NodeInstance:
                        throw new NotImplementedException();
                    case PropertyTemplateType.Bool:
                        return Convert.ToBoolean(ValueInt);
                    case PropertyTemplateType.Stopbits:
                    case PropertyTemplateType.Numeric:
                        return ValueDouble;

                    case PropertyTemplateType.VisuMobilePage:
                        throw new NotImplementedException();

                    case PropertyTemplateType.Scan:
                        return null;
                    case PropertyTemplateType.Invalid:
                        return "INVALID";

                    default:
                        throw new NotImplementedException();
                }

            }
            set
            {
                if(value == null)
                {
                    return;
                }
                switch ((PropertyTemplateType)Type)
                {
                    case PropertyTemplateType.Text:
                    case PropertyTemplateType.Parity:
                    case PropertyTemplateType.Interface:
                    case PropertyTemplateType.Ip:
                    case PropertyTemplateType.Color:
                    case PropertyTemplateType.Time:
                        if (value is DateTime dt)
                        {
                            ValueText = dt.ToString("o");
                        }
                        else
                        {
                            ValueText = value?.ToString();
                        }

                        break;
                    case PropertyTemplateType.NodeInstance:
                        throw new NotImplementedException();
                    case PropertyTemplateType.VisuMobilePage:
                        throw new NotImplementedException();
                    case PropertyTemplateType.DropDown:
                    case PropertyTemplateType.Baudrate:
                    case PropertyTemplateType.Databits:
                    case PropertyTemplateType.Integer:
                    case PropertyTemplateType.Enum:
                    case PropertyTemplateType.Range:
                        if (int.TryParse(value.ToString(), out int result))
                        {
                            ValueInt = result;
                        }
                        else
                        {
                            ValueInt = 0;
                        }
                        break;
                    case PropertyTemplateType.Bool:
                        if (bool.TryParse(value.ToString(), out bool bResult))
                        {
                            ValueInt = bResult ? 1 : 0;
                        }
                        else
                        {
                            ValueInt = 0;
                        }
                        break;
                    case PropertyTemplateType.Stopbits:
                    case PropertyTemplateType.Numeric:
                        ValueDouble = Convert.ToDouble(value);
                        break;
                }
            }
        }
    }
}
