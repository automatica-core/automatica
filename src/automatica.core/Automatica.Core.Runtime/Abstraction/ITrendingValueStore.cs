using Automatica.Core.EF.Models.Trendings;

namespace Automatica.Core.Runtime.Abstraction
{
    internal interface ITrendingValueStore
    {
        void Add(Trending value);
    }
}
