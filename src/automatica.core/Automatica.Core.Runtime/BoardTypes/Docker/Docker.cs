using System;
using System.Collections.Generic;
using Automatica.Core.Base.Common;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Runtime.BoardTypes.Docker
{
    public sealed class Docker : IDatabaseBoardType
    {
        public BoardTypeEnum BoardType => BoardTypeEnum.Docker;
        public InterfaceTypeEnum[] ProvidesInterfaceTypes => new[]{ InterfaceTypeEnum.Ethernet, InterfaceTypeEnum.Virtual, InterfaceTypeEnum.RemoteUsb};

        public static bool InDocker => ServerInfo.InDocker;


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
                ObjId = new Guid("c4f2acd0-051e-4a1b-9222-55c8bd4d1183"),
                This2BoardType = this2BoardType,
                This2InterfaceType = GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.RemoteUsb),
                Name = "Remote",
                Description = "Remote",
                Meta = "remote://"
            };

            list.Add(virt);
            list.Add(eth);
            list.Add(remote);

            return list;
        }

        public string Name => "Docker";
    }
}
