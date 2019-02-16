namespace Automatica.Core.Model
{
    public class TypedObject
    {
        public virtual string TypeInfo => GetType().Name;
    }
}
