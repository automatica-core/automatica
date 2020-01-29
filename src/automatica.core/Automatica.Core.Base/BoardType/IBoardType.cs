using Automatica.Core.Base.Templates;

namespace Automatica.Core.Base.BoardType
{
    public interface IBoardType
    {
        BoardTypeEnum BoardType { get; }
        InterfaceTypeEnum[] ProvidesInterfaceTypes { get; }

        string Name { get; }
    }
}
