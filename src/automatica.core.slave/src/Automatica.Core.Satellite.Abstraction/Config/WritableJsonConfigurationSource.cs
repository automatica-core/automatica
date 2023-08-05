using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Satellite.Abstraction.Config
{
    public class WritableJsonConfigurationSource : JsonConfigurationSource
    {
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            this.EnsureDefaults(builder);
            return (IConfigurationProvider)new WritableJsonConfigurationProvider(this);
        }
    }
}
