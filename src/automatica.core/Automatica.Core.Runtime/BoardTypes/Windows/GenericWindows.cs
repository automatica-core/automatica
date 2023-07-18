using System;
using System.Collections.Generic;
using Automatica.Core.Base.Common;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Runtime.BoardTypes.Windows
{
    public sealed class GenericWindows : IDatabaseBoardType
    {
        public BoardTypeEnum BoardType => BoardTypeEnum.Docker;
        public InterfaceTypeEnum[] ProvidesInterfaceTypes => new[]{ InterfaceTypeEnum.Ethernet, InterfaceTypeEnum.Virtual, InterfaceTypeEnum.RemoteUsb, InterfaceTypeEnum.Usb };


        public IList<BoardInterface> GetBoardInterfaces()
        {
            var list = new List<BoardInterface>();

            var this2BoardType = new Guid("d4c6fa89-a031-4e0a-adb1-8e5824075290");

            var virt = new BoardInterface
            {
                ObjId = new Guid("643d4e12-fccb-4157-beba-ead79e29272d"),
                This2BoardType = this2BoardType,
                This2InterfaceType = GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Virtual),
                Name = "Virtual",
                Description = "Virtual",
                Meta = "virt://"
            };

            var eth = new BoardInterface
            {
                ObjId = new Guid("fb138e53-10d0-4395-adf4-bd7ac2bee177"),
                This2BoardType = this2BoardType,
                This2InterfaceType = GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet),
                Name = "Ethernet",
                Description = "Ethernet",
                Meta = "eth://"
            };
            var remote = new BoardInterface
            {
                ObjId = new Guid("0d14880d-f476-4a55-8d8e-d9fa10adf4be"),
                This2BoardType = this2BoardType,
                This2InterfaceType = GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.RemoteUsb),
                Name = "Remote",
                Description = "Remote",
                Meta = "remote://"
            }; var usb1 = new BoardInterface
            {
                ObjId = new Guid("1bb8d7a9-2220-45ab-8bdb-1a3f1968d9a9"),
                This2BoardType = this2BoardType,
                This2InterfaceType = GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Usb),
                Name = "USB1",
                Description = "USB1",
                Meta = "/dev/ttyUSB0"
            };
            var usb2 = new BoardInterface
            {
                ObjId = new Guid("813c4a5b-707e-4e73-8be0-b2897241cf2d"),
                This2BoardType = this2BoardType,
                This2InterfaceType = GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Usb),
                Name = "USB2",
                Description = "USB2",
                Meta = "/dev/ttyUSB0"
            };


            list.Add(virt);
            list.Add(eth);
            list.Add(remote);
            list.Add(usb1);
            list.Add(usb2);

            return list;
        }

        public string Name => "Windows";
    }
}
