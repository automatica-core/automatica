using Automatica.Core.EF.Models;
using Automatica.Core.Shading.Definition;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Shading.Engine
{
    internal sealed class EfShadingStore : IShadingStore
    {
        private readonly IDbContextFactory<AutomaticaContext> _contextFactory;
        private readonly ILogger<EfShadingStore> _logger;

        public EfShadingStore(IDbContextFactory<AutomaticaContext> contextFactory, ILogger<EfShadingStore> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<ShadingConfiguration> LoadAsync(CancellationToken token = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(token);

            var settingsRow = await context.ShadingSettings.AsNoTracking().SingleOrDefaultAsync(token);
            var facades = await context.ShadingFacades.AsNoTracking().ToListAsync(token);
            var blinds = await context.ShadingBlinds.AsNoTracking().ToListAsync(token);

            return new ShadingConfiguration
            {
                Settings = Parse<ShadingSettingsDefinition>(settingsRow?.Definition, "settings"),
                Facades = facades.Select(a => new FacadeConfig(a.ObjId, a.Name, a.Azimuth, a.IsEnabled, Parse<FacadeDefinition>(a.Definition, a.Name))).ToList(),
                Blinds = blinds.Select(a => new BlindConfig(a.This2RuleInstance, a.This2ShadingFacade, a.IsEnabled, Parse<BlindDefinition>(a.Definition, a.This2RuleInstance.ToString()), a.IsPaused)).ToList()
            };
        }

        public async Task SetPausedAsync(Guid controlId, bool paused, CancellationToken token = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(token);
            var blind = await context.ShadingBlinds.SingleOrDefaultAsync(a => a.This2RuleInstance == controlId, token);
            if (blind == null || blind.IsPaused == paused)
            {
                return;
            }

            blind.IsPaused = paused;
            await context.SaveChangesAsync(token);
        }

        private T Parse<T>(string json, string name) where T : class, new()
        {
            try
            {
                return ShadingJson.Parse<T>(json);
            }
            catch (ShadingDefinitionException e)
            {
                // a broken document must not stop the other blinds
                _logger.LogError(e, $"Shading: invalid definition of {name}, using the defaults");
                return ShadingJson.Parse<T>(null);
            }
        }
    }
}
