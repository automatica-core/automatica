using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automatica.Driver.Shelly.Common
{
    public interface IShellyResultGen2<T> 
    {
        T Result { get; set; }
    }
}
