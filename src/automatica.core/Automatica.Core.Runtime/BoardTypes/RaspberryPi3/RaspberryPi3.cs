using System;
using System.Collections.Generic;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Runtime.BoardTypes.RaspberryPi3
{
    public sealed class RaspberryPi3 : IBoardType
    {
        public BoardTypeEnum BoardType => BoardTypeEnum.RaspberryPi3;

        public IList<BoardInterface> GetBoardInterfaces()
        {
            var list = new List<BoardInterface>();

            var this2BoardType = new Guid("2153e8f3f0e0428b9713a17855795179");

            var virt = new BoardInterface
            {
                ObjId = new Guid("840e94cfa86b483b87a47226d0494c99"),
                This2BoardType = this2BoardType,
                This2InterfaceType = new Guid("177a91443f074fd2a71d51db61c51ad5"),
                Name = "Virtual",
                Description = "Virtual",
                Meta = "virt://"
            };

            var usb1 = new BoardInterface
            {
                ObjId = new Guid("4e70490c4e104c20b76c27032d2bd318"),
                This2BoardType = this2BoardType,
                This2InterfaceType = new Guid("4a02532b4aa04b4ba6a77a0ab6dff5bd"),
                Name = "USB1",
                Description = "USB1",
                Meta = "/dev/ttyUSB0"
            };
            var usb2 = new BoardInterface
            {
                ObjId = new Guid("b9e3446faa3b4f23b50faf36e859e785"),
                This2BoardType = this2BoardType,
                This2InterfaceType = new Guid("4a02532b4aa04b4ba6a77a0ab6dff5bd"),
                Name = "USB2",
                Description = "USB2",
                Meta = "/dev/ttyUSB1"
            };
            var usb3 = new BoardInterface
            {
                ObjId = new Guid("f843ae5d95d44336909dae37de835266"),
                This2BoardType = this2BoardType,
                This2InterfaceType = new Guid("4a02532b4aa04b4ba6a77a0ab6dff5bd"),
                Name = "USB3",
                Description = "USB3",
                Meta = "/dev/ttyUSB2"
            };
            var usb4 = new BoardInterface
            {
                ObjId = new Guid("13101395b05c42be9c0b7915303f9de4"),
                This2BoardType = this2BoardType,
                This2InterfaceType = new Guid("4a02532b4aa04b4ba6a77a0ab6dff5bd"),
                Name = "USB4",
                Description = "USB4",
                Meta = "/dev/ttyUSB3"
            };

            var eth = new BoardInterface
            {
                ObjId = new Guid("6a210ef8a7e646058d1b1eb5752e6080"),
                This2BoardType = this2BoardType,
                This2InterfaceType = new Guid("c45eda9672464fa092399ebb52e7ed66"),
                Name = "Ethernet",
                Description = "Ethernet",
                Meta = "eth://"
            };

            list.Add(virt);
            list.Add(usb1);
            list.Add(usb2);
            list.Add(usb3);
            list.Add(usb4);
            list.Add(eth);

            return list;
        }
    }
}
