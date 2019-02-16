using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Automatica.Core.Model;

namespace Automatica.Core.EF.Models.Categories
{
    public partial class CategoryInstance : TypedObject
    {
        public static readonly Guid Alarm = new Guid("65a8973a-4806-4e23-a8fe-0d7087ad32bd");
        public static readonly Guid Audio = new Guid("d6c66a1e-5e7e-4cfc-9896-b40fff5f1392");
        public static readonly Guid Lightning = new Guid("b0e6963d-91e2-4081-9832-3d8515f78f59");
        public static readonly Guid Shading = new Guid("3d760eac-d703-4a00-a218-fe01d18c429c");
        public static readonly Guid Energy = new Guid("f163caa0-101d-4e0d-ae4e-a2cb1c788e30");
        public static readonly Guid Window = new Guid("1b03f98a-9d59-46c7-9657-cac64e1a9cfb");
        public static readonly Guid Heating = new Guid("453168a0-daa8-4267-8e0c-feaf3c9736f0");
        public static readonly Guid Climate = new Guid("489f5891-1e09-4ae9-8abc-06c10939d73d");
        public static readonly Guid Multimedia = new Guid("668ad0ed-9c52-4eea-aab2-47b69a52e571");
        public static readonly Guid Photovoltaik = new Guid("ab027cdf-de8f-4016-97e4-47492eb292cc");
        public static readonly Guid Power = new Guid("c0e48ce6-59e8-47cc-9a02-48f6958a5901");
        public static readonly Guid Technology = new Guid("8013fe35-6278-43bc-a404-b82dea3b1d7a");
        public static readonly Guid Temperature = new Guid("65513ed3-b622-4b43-acad-a4a3a4f97725");
        public static readonly Guid Weather = new Guid("88c30b85-3335-48cf-8d5e-9531d538a3ad");
        

        public static void GenerateDefault(AutomaticaContext context)
        {
            GenerateIfNotExists(context, Alarm, CategoryGroup.OtherGroup, "ALARM", "alarm-clock");
            GenerateIfNotExists(context, Audio, CategoryGroup.OtherGroup, "AUDIO", "volume-up");
            GenerateIfNotExists(context, Lightning, CategoryGroup.LightningGroup, "LIGHTNING", "lightbulb");
            GenerateIfNotExists(context, Shading, CategoryGroup.ShadingGroup, "SHADING", "th-large");
            GenerateIfNotExists(context, Energy, CategoryGroup.OtherGroup, "ENERGY", "plug");
            GenerateIfNotExists(context, Window, CategoryGroup.OtherGroup, "WINDOW", "square");
            GenerateIfNotExists(context, Heating, CategoryGroup.HvacGroup, "HEATING", "temperature-hot");
            GenerateIfNotExists(context, Climate, CategoryGroup.HvacGroup, "CLIMATE", "temperature-frigid");
            GenerateIfNotExists(context, Multimedia, CategoryGroup.OtherGroup, "MULTIMEDIA", "compact-disc");
            GenerateIfNotExists(context, Photovoltaik, CategoryGroup.OtherGroup, "PHOTOVOLTAIK", "solar-panel");
            GenerateIfNotExists(context, Power, CategoryGroup.OtherGroup, "POWER", "bolt");
            GenerateIfNotExists(context, Technology, CategoryGroup.OtherGroup, "TECHNOLOGY", "memory");
            GenerateIfNotExists(context, Temperature, CategoryGroup.OtherGroup, "TEMPERATURE", "thermometer");
            GenerateIfNotExists(context, Weather, CategoryGroup.OtherGroup, "WEATHER", "sun");
        }

        private static void GenerateIfNotExists(AutomaticaContext context, Guid objId, Guid category, string name, string icon)
        {
            CategoryInstance group = context.CategoryInstances.SingleOrDefault(a => a.ObjId == objId);

            bool isNew = false;
            if (group == null)
            {
                isNew = true;
                group = new CategoryInstance()
                {
                    ObjId = objId,
                    Color = "rgba(255, 255, 255, 1)",
                    IsFavorite = false,
                    Rating = 1,
                    Icon = icon,
                    IsDeleteable = false
                };
            }

            group.Name = $"COMMON.CATEGORY_INSTANCE.{name.ToUpperInvariant()}.NAME";
            group.Description = $"COMMON.CATEGORY_INSTANCE.{name.ToUpperInvariant()}.DESCRIPTION";
            group.This2CategoryGroup = category;
            group.IsDeleteable = false;
            group.Icon = icon;

            if (isNew)
            {
                context.CategoryInstances.Add(group);
            }
            else
            {
                context.CategoryInstances.Update(group);
            }
        }

    }
}
