using System;
using System.Collections.Generic;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Runtime.BoardTypes.Docker
{
    public sealed class Docker : IBoardType
    {
        public BoardTypeEnum BoardType => BoardTypeEnum.Docker;

        internal static bool InDocker { get { return Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"; } }


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
            list.Add(eth);

            return list;
        }

        public string Name => "Docker";
    }
}
