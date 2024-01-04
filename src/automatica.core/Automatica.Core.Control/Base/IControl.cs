using Automatica.Core.Model;

namespace Automatica.Core.Control.Base
{
    public interface IControl : ITypedObject
    {
        Guid Id { get;  }
        string Name { get; }

        public void RegisterValueChanged(Action<IControl> callback);
        string ITypedObject.TypeInfo => "Control";
    }
}
