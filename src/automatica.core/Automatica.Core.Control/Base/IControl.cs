using Automatica.Core.Model;

namespace Automatica.Core.Control.Base
{
    public interface IControl : ITypedObject
    {
        Guid Id { get;  }
        string Name { get; }

        string ITypedObject.TypeInfo => "Control";
    }
}
