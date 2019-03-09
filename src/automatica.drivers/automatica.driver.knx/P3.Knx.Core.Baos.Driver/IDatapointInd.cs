using P3.Knx.Core.Baos.Driver.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace P3.Knx.Core.Baos.Driver
{
    public interface IDatapointInd
    {
        Task DatapointInd(IReadOnlyCollection<DatapointValue> values);
    }
}
