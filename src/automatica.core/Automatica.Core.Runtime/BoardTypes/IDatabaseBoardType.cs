using System.Collections.Generic;
using Automatica.Core.Base.BoardType;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Runtime.BoardTypes
{
    public interface IDatabaseBoardType : IBoardType
    {
        IList<BoardInterface> GetBoardInterfaces();
    }
}
