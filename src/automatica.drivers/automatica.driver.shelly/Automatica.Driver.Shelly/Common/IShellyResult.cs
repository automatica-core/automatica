using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automatica.Driver.Shelly.Common
{
    public interface IShellyResult<T>
    {
        T Value { get; }
        bool IsSuccess { get; }
    }
}
