using System;
using System.Collections.Generic;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Runtime.BoardTypes.RaspberryPi
{
    public sealed class RaspberryPi : IDatabaseBoardType
    {
        public BoardTypeEnum BoardType => BoardTypeEnum.RaspberryPi3;
        public InterfaceTypeEnum[] ProvidesInterfaceTypes => new[] { InterfaceTypeEnum.Ethernet, InterfaceTypeEnum.Virtual, InterfaceTypeEnum.Usb, InterfaceTypeEnum.RemoteUsb };
        public string Name => "RaspberryPi";

        public IList<BoardInterface> GetBoardInterfaces()
        {
            var list = new List<BoardInterface>();

            var this2BoardType = new Guid("2153e8f3f0e0428b9713a17855795179");

            var virt = new BoardInterface
            {
                ObjId = new Guid("840e94cfa86b483b87a47226d0494c99"),
                This2BoardType = this2BoardType,
                This2InterfaceType = GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Virtual),
                Name = "Virtual",
                Description = "Virtual",
                Meta = "virt://"
            };

            var usb1 = new BoardInterface
            {
                ObjId = new Guid("4e70490c4e104c20b76c27032d2bd318"),
                This2BoardType = this2BoardType,
                This2InterfaceType = GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Usb),
                Name = "USB1",
                Description = "USB1",
                Meta = "/dev/ttyUSB0"
            };
            var usb2 = new BoardInterface
            {
                ObjId = new Guid("b9e3446faa3b4f23b50faf36e859e785"),
                This2BoardType = this2BoardType,
                This2InterfaceType = GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Usb),
                Name = "USB2",
                Description = "USB2",
                Meta = "/dev/ttyUSB1"
            };
            var usb3 = new BoardInterface
            {
                ObjId = new Guid("f843ae5d95d44336909dae37de835266"),
                This2BoardType = this2BoardType,
                This2InterfaceType = GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Usb),
                Name = "USB3",
                Description = "USB3",
                Meta = "/dev/ttyUSB2"
            };
            var usb4 = new BoardInterface
            {
                ObjId = new Guid("13101395b05c42be9c0b7915303f9de4"),
                This2BoardType = this2BoardType,
                This2InterfaceType = GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Usb),
                Name = "USB4",
                Description = "USB4",
                Meta = "/dev/ttyUSB3"
            };

            var eth = new BoardInterface
            {
                ObjId = new Guid("6a210ef8a7e646058d1b1eb5752e6080"),
                This2BoardType = this2BoardType,
                This2InterfaceType = GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet),
                Name = "Ethernet",
                Description = "Ethernet",
                Meta = "eth://"
            };
            var remote = new BoardInterface
            {
                ObjId = new Guid("b5a29960-45a9-40b1-a656-8adcbb110039"),
                This2BoardType = this2BoardType,
                This2InterfaceType = GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.RemoteUsb),
                Name = "Remote",
                Description = "Remote",
                Meta = "remote://"
            };

            list.Add(virt);
            list.Add(usb1);
            list.Add(usb2);
            list.Add(usb3);
            list.Add(usb4);
            list.Add(eth);
            list.Add(remote);

            return list;
        }
    }
}
