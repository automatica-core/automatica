using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Automatica.Core.Runtime.BoardTypes
{
    internal interface IBoardType
    {
        BoardTypeEnum BoardType { get; }
        IList<BoardInterface> GetBoardInterfaces();
    }
}
