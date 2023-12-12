using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using Automatica.Core.Model;

namespace Automatica.Core.EF.Models
{
    public class PropertyTemplateTypeAttribute : Attribute {
        public PropertyTemplateTypeAttribute(string name, string description, string meta=null)
        {
            Name = name;
            Description = description;
            Meta = meta;
        }

        public PropertyTemplateTypeAttribute(string name, string meta = null)
        {
            Name = $"PROPERTY_TEMPLATE_TYPE.{name}.NAME";
            Description = $"PROPERTY_TEMPLATE_TYPE.{name}.DESCRIPTION";
            Meta = meta;
        }

        public string Name { get;  }
        public string Description { get;  }
        public string Meta { get;  }
    }
    public enum PropertyTemplateType
    {
        [PropertyTemplateType("TEXT")]
        Text = 0,
        [PropertyTemplateType("INTEGER")]
        Integer = 1,
        [PropertyTemplateType("NUMERIC")]
        Numeric = 2,
        [PropertyTemplateType("DROPDOWN")]
        DropDown = 3,
        [PropertyTemplateType("BOOLEAN")]
        Bool = 4,
        [PropertyTemplateType("IP")]
        Ip = 5,
        [PropertyTemplateType("INTERFACE")]
        Interface = 6,
        [PropertyTemplateType("BAUDRATE", "2400,9600,19200,38400,57600,115200")]
        Baudrate = 7,
        [PropertyTemplateType("PARITY", "NoParity|n,EventParity|ep,OddParity|o")]
        Parity = 8,
        [PropertyTemplateType("DATABITS", "Data5|5,Data6|6,Data7|7,Data8|8")]
        Databits = 9,
        [PropertyTemplateType("STOPBITS", "OneStop|1,OneAndHalfStop|1.5,TwoStop|2")]
        Stopbits = 10,

        [PropertyTemplateType("SCAN")]
        Scan = 11,
        [PropertyTemplateType("ENUM")]
        Enum = 12,
        [PropertyTemplateType("RANGE")]
        Range = 13,

        [PropertyTemplateType("COLOR")]
        Color = 14,

        [PropertyTemplateType("NODE_INSTANCE")]
        NodeInstance = 15,
        [PropertyTemplateType("VISU_MOBILE_PAGE")]
        VisuMobilePage = 16,

        [PropertyTemplateType("USB_PORT")]
        UsbPort = 17,
        [PropertyTemplateType("LONG")]
        Long = 18,
        [PropertyTemplateType("PASSWORD")]
        Password = 19,

        [PropertyTemplateType("AREA_INSTANCE_LINK")]
        AreaInstanceLink = 20,
        [PropertyTemplateType("CATEGORY_INSTANCE_LINK")]
        CategoryInstanceLink = 21,

        [PropertyTemplateType("IMPORT_DATA")]
        ImportData = 22,
        [PropertyTemplateType("LEARN_MODE")]
        LearnMode = 23,
        [PropertyTemplateType("TIMER")]
        Timer = 24,
        [PropertyTemplateType("TIME")]
        Time = 25,
        [PropertyTemplateType("DATETIME")]
        DateTime = 26,
        [PropertyTemplateType("CALENDAR")]
        Calendar = 27,

        [PropertyTemplateType("AREA_ICON", "shoe-prints,alarm-clock,volume-up,lightbulb,th-large,plug,square,temperature-hot,temperature-frigid,compact-disc,solar-panel,bolt,memory,thermometer,sun,home,project-diagram,building,box,bed,tv,bath,fork-knife,apple-core,heat,toilet-paper-blank,toilet-portable,bed-front,bed-bunk,dryer-heat,fireplace,air-conditioner,wind,industry,outlet,charging-station,poop,fan")]
        AreaIcon = 100,
        [PropertyTemplateType("AREA")]
        AreaInstance = 101,
        [PropertyTemplateType("CategoryInstance")]
        CategoryInstance = 102,

        [PropertyTemplateType("User2Groups")]
        User2Groups = 200,
        [PropertyTemplateType("User2Roles")]
        User2Roles = 201,
        [PropertyTemplateType("UserGroup2Roles")]
        UserGroup2Roles = 202,
        [PropertyTemplateType("UserGroup")]
        UserGroup = 203,
        [PropertyTemplateType("Slave")]
        Slave = 204,
        
        [PropertyTemplateType("MultiSelect")]
        MultiSelect = 300,

        [PropertyTemplateType("CustomAction")]
        CustomAction = 500,
        




        [PropertyTemplateType("INVALID")]
        Invalid = 2147483647
    }

    public partial class PropertyInstance : TypedObject
    {
        [NotMapped]
        public PropertyTemplateType PropertyType
        {
            get
            {
                if (This2PropertyTemplateNavigation != null)
                {
                    return (PropertyTemplateType)This2PropertyTemplateNavigation.This2PropertyTypeNavigation.Type;
                }
                return PropertyTemplateType.Invalid;
            }
        }

        [NotMapped]
        public object Value
        {
            get
            {
                switch (PropertyType)
                {
                    case PropertyTemplateType.Long:
                        return ValueLong;
                    case PropertyTemplateType.Text:
                    case PropertyTemplateType.Parity:
                    case PropertyTemplateType.Interface:
                    case PropertyTemplateType.Ip:
                    case PropertyTemplateType.Color:
                    case PropertyTemplateType.UsbPort:
                    case PropertyTemplateType.AreaIcon:
                    case PropertyTemplateType.Password:
                    case PropertyTemplateType.UserGroup:
                    case PropertyTemplateType.Time:
                    case PropertyTemplateType.MultiSelect:
                        return ValueString;
                    case PropertyTemplateType.DropDown:
                    case PropertyTemplateType.Baudrate:
                    case PropertyTemplateType.Databits:
                    case PropertyTemplateType.Integer:
                    case PropertyTemplateType.Enum:
                    case PropertyTemplateType.Range:
                        return ValueInt;
                    case PropertyTemplateType.NodeInstance:
                        return ValueNodeInstance;
                    case PropertyTemplateType.Bool:
                        return ValueBool;
                    case PropertyTemplateType.Stopbits:
                    case PropertyTemplateType.Numeric:
                        return ValueDouble;

                    case PropertyTemplateType.VisuMobilePage:
                        return ValueVisuPage;
                    case PropertyTemplateType.AreaInstanceLink:
                        return ValueAreaInstance;
                    case PropertyTemplateType.CategoryInstanceLink:
                        return ValueCategoryInstance;
                    case PropertyTemplateType.Slave:
                        return ValueSlave;

                    case PropertyTemplateType.Scan:
                    case PropertyTemplateType.ImportData:
                    case PropertyTemplateType.LearnMode:
                    case PropertyTemplateType.CustomAction:
                        return null;
                    case PropertyTemplateType.Invalid:
                        return "INVALID";
                    
                    default:
                        throw new NotImplementedException();
                }
            }
            set
            {

                if (value == null)
                {
                    return;
                }
                switch (PropertyType)
                {
                    case PropertyTemplateType.Long:
                        if (long.TryParse(value.ToString(), out long llResult))
                        {
                            ValueLong = llResult;
                        }
                        else
                        {
                            ValueLong = null;
                        }
                        break;
                        
                    case PropertyTemplateType.Text:
                    case PropertyTemplateType.Parity:
                    case PropertyTemplateType.Interface:
                    case PropertyTemplateType.Ip:
                    case PropertyTemplateType.Color:
                    case PropertyTemplateType.UsbPort:
                    case PropertyTemplateType.AreaIcon:
                    case PropertyTemplateType.Password:
                    case PropertyTemplateType.Time:
                    case PropertyTemplateType.UserGroup:
                    case PropertyTemplateType.MultiSelect:
                        ValueString = value.ToString();
                        break;
                    case PropertyTemplateType.NodeInstance:
                        if (Guid.TryParse(value.ToString(), out Guid gResult))
                        {
                            ValueNodeInstance = gResult;
                        }
                        else
                        {
                            ValueNodeInstance = null;
                        }
                        break;
                    case PropertyTemplateType.VisuMobilePage:
                        if (Guid.TryParse(value.ToString(), out Guid lResult2))
                        {
                            ValueVisuPage = lResult2;
                        }
                        else
                        {
                            ValueVisuPage = null;
                        }
                        break;
                    case PropertyTemplateType.AreaInstanceLink:
                        if (Guid.TryParse(value.ToString(), out Guid lResult3))
                        {
                            ValueAreaInstance = lResult3;
                        }
                        else
                        {
                            ValueAreaInstance = null;
                        }
                        break;
                    case PropertyTemplateType.CategoryInstanceLink:
                        if (Guid.TryParse(value.ToString(), out Guid lResult4))
                        {
                            ValueCategoryInstance = lResult4;
                        }
                        else
                        {
                            ValueCategoryInstance = null;
                        }
                        break;
                    case PropertyTemplateType.Slave:
                        if (Guid.TryParse(value.ToString(), out Guid lResult5))
                        {
                            ValueSlave = lResult5;
                        }
                        else
                        {
                            ValueSlave = null;
                        }
                        break;
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
                        else if (value is Enum enumValue)
                        {
                            ValueInt = (int) Enum.ToObject(enumValue.GetType(), enumValue);
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
                            ValueBool = bResult;
                        }
                        else
                        {
                            ValueInt = 0;
                        }
                        break;
                    case PropertyTemplateType.Stopbits:
                    case PropertyTemplateType.Numeric:
                        if (value is string strValue && string.IsNullOrEmpty(strValue))
                        {
                            ValueDouble = null;
                            ValueInt = null;
                        }
                        else
                        {
                            ValueDouble = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                            if (ValueDouble <= int.MaxValue)
                            {
                                ValueInt = Convert.ToInt32(ValueDouble);
                            }
                            else
                            {
                                ValueLong = Convert.ToInt64(ValueDouble);
                            }
                        }

                        break;
                }
            }
        }
    }
}
