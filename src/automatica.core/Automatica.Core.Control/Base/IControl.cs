using Automatica.Core.Model;

namespace Automatica.Core.Control.Base
{
    public interface IControlValueCallback
    {
        public Guid RegisterValueCallback(Action callback);
        public void UnregisterValueCallback(Guid id);
    }
    
    public interface IControl : ITypedObject
    {
        Guid Id { get;  }
        string Name { get; }

        string ITypedObject.TypeInfo => "Control";
    }
}
