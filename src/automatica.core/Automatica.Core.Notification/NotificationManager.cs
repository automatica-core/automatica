using Automatica.Core.EF.Models;

namespace Automatica.Core.Notification
{
    internal class NotificationManager(AutomaticaContext dbContext) : INotificationManager
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
            dbContext.Notifications.Add(notification);
            await dbContext.SaveChangesAsync(token);
        }

        public Task DismissNotification(Guid notificationId, CancellationToken token = default)
        {
            var notification = dbContext.Notifications.SingleOrDefault(a => a.ObjId == notificationId);

            if (notification == null)
            {
                return Task.CompletedTask;
            }

            return DismissNotification(notification, token);
        }

        public Task DismissNotification(EF.Models.Notification notification, CancellationToken token = default)
        {
            notification.DismissDate = DateTime.Now;
            dbContext.Notifications.Update(notification);
            return Task.CompletedTask;
        }
    }
}
