using System;

// ReSharper disable InconsistentNaming

namespace Automatica.Core.Base.Templates
{
    public class GuidTemplateTypeAttribute : Attribute
    {
        public GuidTemplateTypeAttribute(string guid, int maxChilds)
        {
            Guid = new Guid(guid);
            MaxChilds = maxChilds;
        }

        public static Guid GetFromEnum(Enum enumValue)
        {
            var type = enumValue.GetType();
            var memInfo = type.GetMember(enumValue.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(GuidTemplateTypeAttribute), false);

            if (attributes.Length > 0 && attributes[0] is GuidTemplateTypeAttribute attribute)
            {
                return attribute.Guid;
            }
            throw new ArgumentException($"enum must provide {nameof(GuidTemplateTypeAttribute)} attribute");
        }

        public Guid Guid { get; }
        public int MaxChilds { get; }
    }

    /// <summary>
    /// Preset InterfaceTypes
    /// </summary>
    public enum InterfaceTypeEnum
    {
        [GuidTemplateType("00000000-0000-0000-0000-000000000001", 0)]
        Value,
        [GuidTemplateType("177a9144-3f07-4fd2-a71d-51db61c51ad5", int.MaxValue)]
        Virtual,
        [GuidTemplateType("c45eda96-7246-4fa0-9239-9ebb52e7ed66", int.MaxValue)]
        Ethernet,
        [GuidTemplateType("4a02532b-4aa0-4b4b-a6a7-7a0ab6dff5bd", 1)]
        Usb,
        [GuidTemplateType("fa0b3410-c472-474a-af67-ab298e07e427", 1)]
        Rs485,
        [GuidTemplateType("d585ecd8-8639-4bc8-a6c7-1641f77a9f08", 1)]
        Rs232,
        [GuidTemplateType("8cfc6b92-0f68-44d4-8709-f340ce48ff1c", 1)]
        UsbIr,
        [GuidTemplateType("4ff72aff-1604-4865-8d40-4d11bbbe2c56", 1)]
        Board
    }

    public enum BoardTypeEnum
    {
        [GuidTemplateType("2153e8f3-f0e0-428b-9713-a17855795179", int.MaxValue)]
        RaspberryPi3,
        [GuidTemplateType("9550a1fd-22fa-427f-8fba-6443fd99fd29", int.MaxValue)]
        Docker
    }
}
