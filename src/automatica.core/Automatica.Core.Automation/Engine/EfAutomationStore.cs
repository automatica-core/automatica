using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Automations;
using Microsoft.EntityFrameworkCore;

namespace Automatica.Core.Automation.Engine
{
    internal sealed class EfAutomationStore : IAutomationStore
    {
        private readonly IDbContextFactory<AutomaticaContext> _contextFactory;

        public EfAutomationStore(IDbContextFactory<AutomaticaContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<IReadOnlyList<EF.Models.Automations.Automation>> LoadAllAsync(CancellationToken token = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(token);
            return await context.Automations.AsNoTracking().ToListAsync(token);
        }

        public async Task<EF.Models.Automations.Automation> LoadAsync(Guid id, CancellationToken token = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(token);
            return await context.Automations.AsNoTracking().SingleOrDefaultAsync(a => a.ObjId == id, token);
        }

        public async Task<IReadOnlyCollection<Guid>> GetIdsAsync(CancellationToken token = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(token);
            return await context.Automations.AsNoTracking().Select(a => a.ObjId).ToListAsync(token);
        }

        public async Task SetEnabledAsync(Guid id, bool enabled, CancellationToken token = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(token);
            var automation = await context.Automations.SingleOrDefaultAsync(a => a.ObjId == id, token);
            if (automation == null)
            {
                return;
            }

            automation.IsEnabled = enabled;
            automation.ModifiedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(token);
        }

        public async Task SaveRunAsync(AutomationRun run, int keep, CancellationToken token = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(token);

            var automation = await context.Automations.SingleOrDefaultAsync(a => a.ObjId == run.This2Automation, token);
            if (automation == null)
            {
                // deleted in the meantime
                return;
            }

            context.AutomationRuns.Add(run);
            automation.LastRunAt = run.StartedAt;
            automation.LastRunState = run.State;
            automation.LastError = run.Error;

            await context.SaveChangesAsync(token);

            var old = await context.AutomationRuns
                .Where(a => a.This2Automation == run.This2Automation)
                .OrderByDescending(a => a.StartedAt)
                .Skip(keep)
                .ToListAsync(token);

            if (old.Count > 0)
            {
                context.AutomationRuns.RemoveRange(old);
                await context.SaveChangesAsync(token);
            }
        }

        public async Task<IReadOnlyList<AutomationRun>> GetRunsAsync(Guid automationId, int take, CancellationToken token = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(token);
            return await context.AutomationRuns.AsNoTracking()
                .Where(a => a.This2Automation == automationId)
                .OrderByDescending(a => a.StartedAt)
                .Take(take)
                .ToListAsync(token);
        }
    }
}
