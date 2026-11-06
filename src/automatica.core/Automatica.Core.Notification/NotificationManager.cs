using Automatica.Core.EF.Models;
using Microsoft.EntityFrameworkCore;

namespace Automatica.Core.Notification
{
    internal class NotificationManager(IDbContextFactory<AutomaticaContext> contextFactory) : INotificationManager
    {
        public Task CreateNotification(NodeInstance nodeInstance, string body, string subject, NotificationSeverity severity, CancellationToken token = default)
        {
            var notification = new EF.Models.Notification
            {
                Body = body,
                Subject = subject,
                CreatedAt = DateTime.Now,
                ObjId = Guid.NewGuid(),
                Severity = severity,
                This2NodeInstance = nodeInstance.ObjId
            };
            return AddAndSaveNotification(notification, token);
        }

        public Task CreateNotification(RuleInstance ruleInstance, string body, string subject, NotificationSeverity severity, CancellationToken token = default)
        {
            var notification = new EF.Models.Notification
            {
                Body = body,
                Subject = subject,
                CreatedAt = DateTime.Now,
                ObjId = Guid.NewGuid(),
                Severity = severity,
                This2RuleInstance = ruleInstance.ObjId
            };
            return AddAndSaveNotification(notification, token);
        }

        private async Task AddAndSaveNotification(EF.Models.Notification notification, CancellationToken token)
        {
            await using var dbContext = await contextFactory.CreateDbContextAsync(token);
            dbContext.Notifications.Add(notification);
            await dbContext.SaveChangesAsync(token);
        }

        public async Task DismissNotification(Guid notificationId, CancellationToken token = default)
        {
            await using var dbContext = await contextFactory.CreateDbContextAsync(token);
            var notification = await dbContext.Notifications.SingleOrDefaultAsync(a => a.ObjId == notificationId, token);

            if (notification == null)
            {
                return;
            }

            notification.DismissDate = DateTime.Now;
            await dbContext.SaveChangesAsync(token);
        }

        public async Task DismissNotification(EF.Models.Notification notification, CancellationToken token = default)
        {
            await using var dbContext = await contextFactory.CreateDbContextAsync(token);
            notification.DismissDate = DateTime.Now;

            // only the notification row (and only the dismiss date), not the attached navigations
            var entry = dbContext.Entry(notification);
            entry.State = EntityState.Unchanged;
            entry.Property(a => a.DismissDate).IsModified = true;
            await dbContext.SaveChangesAsync(token);
        }
    }
}
