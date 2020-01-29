using System;
using System.Linq;
using Automatica.Core.Model;

namespace Automatica.Core.EF.Models.Categories
{
    public partial class CategoryGroup : TypedObject
    {
        public static readonly Guid ShadingGroup = new Guid("6f1382e0-ac94-47ea-9b81-60c8fc2c5ec6");
        public static readonly Guid LightningGroup = new Guid("2b3a1e6d-e5e1-4395-b76d-dd787d1f17b3");
        public static readonly Guid HvacGroup = new Guid("76caeb29-7904-41c4-a2d9-45689f2b713b");
        public static readonly Guid MultimediaGroup = new Guid("caa106d8-4bcb-4e39-925b-b8c5462a5169");
        public static readonly Guid OtherGroup = new Guid("f61737b4-f948-4d41-9af0-42b93997ee0d");


        public static void GenerateDefault(AutomaticaContext context)
        {
            GenerateIfNotExists(context, ShadingGroup, "SHADING");
            GenerateIfNotExists(context, LightningGroup, "LIGHTNING");
            GenerateIfNotExists(context, HvacGroup, "HVAC");
            GenerateIfNotExists(context, MultimediaGroup, "MULTIMEDIA");
            GenerateIfNotExists(context, OtherGroup, "OTHER");
        }

        public static void GenerateIfNotExists(AutomaticaContext context, Guid objId, string name)
        {
            CategoryGroup group = context.CategoryGroups.SingleOrDefault(a => a.ObjId == objId);

            bool isNew = false;
            if (group == null)
            {
                isNew = true;
                group = new CategoryGroup()
                {
                    ObjId = objId
                };
            }

            group.Name = $"COMMON.CATEGORY_GROUP.{name.ToUpperInvariant()}.NAME";
            group.Description = $"COMMON.CATEGORY_GROUP.{name.ToUpperInvariant()}.DESCRIPTION";

            if (isNew)
            {
                context.CategoryGroups.Add(group);
            }
            else
            {
                context.CategoryGroups.Update(group);
            }
        }




    }
}
