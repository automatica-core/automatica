using System;
using System.Collections.Generic;
using Automatica.Core.Model;
using Newtonsoft.Json;

namespace Automatica.Core.EF.Models
{
    public enum VisuPageTypeEnumeration
    {
        Pc,
        Mobile
    }
    public partial class VisuObjectInstance : TypedObject
    {
        public override string TypeInfo
        {
            get
            {
                if (This2VisuObjectTemplateNavigation == null)
                {
                    return base.TypeInfo;
                }

                if (This2VisuObjectTemplateNavigation.This2VisuPageType == (int)VisuPageTypeEnumeration.Mobile)
                {
                    return base.TypeInfo+"Mobile";
                }
                return base.TypeInfo;
            }
        }
    }
}
