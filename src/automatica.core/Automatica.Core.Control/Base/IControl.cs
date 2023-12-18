using Automatica.Core.Model;

namespace Automatica.Core.Control.Base
{
    public interface IControl : ITypedObject
    {
        public Guid Id { get;  }
        public string Name { get; }

        public new string TypeInfo => "Control";
    }
}
