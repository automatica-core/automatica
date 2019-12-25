using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using System.Collections.Generic;

namespace Automatica.Core.Runtime.BoardTypes
{
    internal interface IBoardType
    {
        BoardTypeEnum BoardType { get; }
        IList<BoardInterface> GetBoardInterfaces();

        string Name { get; }
    }
}
