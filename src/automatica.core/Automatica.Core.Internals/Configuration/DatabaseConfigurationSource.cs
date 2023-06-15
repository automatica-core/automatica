using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Configuration
{
    public class DatabaseConfigurationSource : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
           return new DatabaseConfigurationProvider();
        }
    }
}
