namespace Automatica.Core.Model
{
    public class TypedObject : ITypedObject
    {
        public virtual string TypeInfo => GetType().Name;
    }
}
