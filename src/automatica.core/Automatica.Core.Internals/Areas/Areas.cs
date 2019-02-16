using System;
using System.Collections.Generic;
using System.Text;

namespace Automatica.Core.Internals.Areas
{
    /// <summary>
    /// Used to define an area type
    /// </summary>
    public sealed class AreaTypeAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }
        public Guid ObjId { get; }

        public AreaTypeAttribute(string name, string objId)
        {
            Name = $"AREA_TYPE.{name.ToUpperInvariant()}.NAME";
            Description = $"AREA_TYPE.{name.ToUpperInvariant()}.DESCRIPTION";
            ObjId = new Guid(objId);
        }

        public static Guid GetFromEnum(AreaTypes areaType)
        {
            var type = areaType.GetType();
            var memInfo = type.GetMember(areaType.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(AreaTypeAttribute), false);

            if (attributes.Length == 1 && attributes[0] is AreaTypeAttribute attribute)
            {
                return attribute.ObjId;
            }

            throw new ArgumentException($"{nameof(areaType)} is no {nameof(AreaTypeAttribute)}");
            
        }
    }
    public enum AreaTypes
    {
        [AreaType("Project", "3ad15c25-e168-45b5-8b8c-74c30040fe14")]
        Project,
        [AreaType("Building", "adcb43ea-119e-4d22-bd2a-59da3c7b8010")]
        Building,
        [AreaType("BuildingPart", "79c84ba6-2fc2-4f1f-bc6c-c2cf6193de85")]
        BuildingPart,
        [AreaType("Room", "4ef553b0-e1cb-45d5-948e-90ad87a2174f")]
        Room,
        [AreaType("Nothing", "8e901dce-205e-46c6-83a8-9ff942392a47")]
        Nothing
    }

    public sealed class AreaTemplateAttribute : Attribute
    {
        public Guid ObjId { get; }
        public string Name { get; }
        public string Description { get; }
        public string Icon { get; }
        public AreaTypes NeedsAreaType { get; }
        public AreaTypes ProvidesAreayType { get; }
        public AreaTypes IsAreaType { get; }
        public bool IsDeletable { get; }

        public AreaTemplateAttribute(string objId, string name, string icon, AreaTypes needsAreaType, AreaTypes providesAreayType, AreaTypes isAreaType, bool isDeletable=true)
        {
            ObjId = new Guid(objId);
            Name = $"AREA_TEMPLATE.{name.ToUpperInvariant()}.NAME";
            Description = $"AREA_TEMPLATE.{name.ToUpperInvariant()}.DESCRIPTION";
            Icon = icon;
            NeedsAreaType = needsAreaType;
            ProvidesAreayType = providesAreayType;
            IsAreaType = isAreaType;
            IsDeletable = isDeletable;
        }


        public static Guid GetFromEnum(AreaTemplates areaType)
        {
            var type = areaType.GetType();
            var memInfo = type.GetMember(areaType.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(AreaTemplateAttribute), false);

            if (attributes.Length == 1 && attributes[0] is AreaTemplateAttribute attribute)
            {
                return attribute.ObjId;
            }

            throw new ArgumentException($"{nameof(areaType)} is no {nameof(AreaTemplateAttribute)}");

        }

        public static AreaTemplateAttribute GetAttributeFromEnum(AreaTemplates areaType)
        {
            var type = areaType.GetType();
            var memInfo = type.GetMember(areaType.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(AreaTemplateAttribute), false);

            if (attributes.Length == 1 && attributes[0] is AreaTemplateAttribute attribute)
            {
                return attribute;
            }

            throw new ArgumentException($"{nameof(areaType)} is no {nameof(AreaTemplateAttribute)}");

        }
    }

    public enum AreaTemplates
    {
        [AreaTemplate("ee98e357-33ed-492c-a203-729c60daa22e", "Project", "home", AreaTypes.Project, AreaTypes.Building, AreaTypes.Project, false)]
        Project,
        [AreaTemplate("a2ec8a7b-1b8e-4dd7-9d4e-9567c1760af2", "Building", "building", AreaTypes.Project, AreaTypes.BuildingPart, AreaTypes.Building)]
        Building,
        [AreaTemplate("8592c6ac-1093-4242-ba53-7d4456cbd22e", "BuildingPart", "box", AreaTypes.Building, AreaTypes.BuildingPart, AreaTypes.BuildingPart)]
        BuildingPart,
        [AreaTemplate("492e20ce-7d6a-44d4-aa07-454851180c7f", "Room", "booth-curtain", AreaTypes.BuildingPart, AreaTypes.BuildingPart, AreaTypes.BuildingPart)]
        Room,
        [AreaTemplate("f6052884-5833-4db7-9f88-225cbcb57002", "Staircase", "shoe-prints", AreaTypes.BuildingPart, AreaTypes.Nothing, AreaTypes.BuildingPart)]
        Staircase,
        [AreaTemplate("60cc7657-a3cc-4956-af1f-f0d24581e9b9", "Hallway", "archway", AreaTypes.BuildingPart, AreaTypes.Nothing, AreaTypes.BuildingPart)]
        Hallway
    }
}
