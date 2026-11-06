using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

[assembly: InternalsVisibleTo("Automatica.Core.Tests")]
[assembly: InternalsVisibleTo("Automatica.Core.WebApi.Tests")]

namespace Automatica.Core.EF.Security
{
    /// <summary>
    /// Transparent encryption of secrets at rest: <see cref="PropertyInstance.ValueString"/> of password properties and
    /// <see cref="Setting.ValueText"/> of the secret settings are encrypted while they are written and decrypted when they are
    /// read, so drivers, caches and the configuration see the plain values. Plain values in old databases are read as they are
    /// and encrypted by <see cref="EncryptExisting"/> (startup) or on their next save.
    /// </summary>
    public sealed class SecretsInterceptor : ISaveChangesInterceptor, IMaterializationInterceptor
    {
        public static readonly SecretsInterceptor Instance = new();

        /// <summary>
        /// Settings that hold credentials (in addition to settings of type password)
        /// </summary>
        public static readonly IReadOnlySet<string> SecretSettingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "apiKey", "mcpApiKey", "hyperSeriesPassword", "hostedGrafanaApiKey", "ngrokToken"
        };

        /// <summary>
        /// Property template id → is a password template (templates never change their type)
        /// </summary>
        private static readonly ConcurrentDictionary<Guid, bool> PasswordTemplates = new();

        private sealed record Pending(PropertyEntry Property, string Plain);

        private readonly ConditionalWeakTable<DbContext, List<Pending>> _pending = new();

        public static bool IsSecretSetting(Setting setting)
        {
            return setting != null && (setting.Type == (long)PropertyTemplateType.Password ||
                                       (setting.ValueKey != null && SecretSettingKeys.Contains(setting.ValueKey)));
        }

        #region read

        public object InitializedInstance(MaterializationInterceptionData materializationData, object entity)
        {
            switch (entity)
            {
                case PropertyInstance property when SecretProtector.IsProtected(property.ValueString):
                    if (SecretProtector.TryUnprotect(property.ValueString, out var plain))
                    {
                        property.ValueString = plain;
                    }
                    break;
                case Setting setting when SecretProtector.IsProtected(setting.ValueText):
                    if (SecretProtector.TryUnprotect(setting.ValueText, out var plainText))
                    {
                        setting.ValueText = plainText;
                    }
                    break;
            }

            return entity;
        }

        #endregion

        #region write

        public InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (eventData.Context != null)
            {
                Protect(eventData.Context);
            }
            return result;
        }

        public ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context != null)
            {
                Protect(eventData.Context);
            }
            return ValueTask.FromResult(result);
        }

        public int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            Restore(eventData.Context);
            return result;
        }

        public ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            Restore(eventData.Context);
            return ValueTask.FromResult(result);
        }

        public void SaveChangesFailed(DbContextErrorEventData eventData)
        {
            Restore(eventData.Context);
        }

        public Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            Restore(eventData.Context);
            return Task.CompletedTask;
        }

        private void Protect(DbContext context)
        {
            // the interceptor runs before SaveChanges detects the changes
            if (context.ChangeTracker.AutoDetectChangesEnabled)
            {
                context.ChangeTracker.DetectChanges();
            }

            List<Pending> pending = null;

            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.State is not (EntityState.Added or EntityState.Modified))
                {
                    continue;
                }

                PropertyEntry valueProperty = entry.Entity switch
                {
                    PropertyInstance property when IsPasswordProperty(context, property) => entry.Property(nameof(PropertyInstance.ValueString)),
                    Setting setting when IsSecretSetting(setting) => entry.Property(nameof(Setting.ValueText)),
                    _ => null
                };

                if (valueProperty == null || (entry.State == EntityState.Modified && !valueProperty.IsModified))
                {
                    continue;
                }

                var plain = valueProperty.CurrentValue as string;
                var protectedValue = SecretProtector.Protect(plain);
                if (ReferenceEquals(protectedValue, plain))
                {
                    continue;
                }

                valueProperty.CurrentValue = protectedValue;
                (pending ??= new List<Pending>()).Add(new Pending(valueProperty, plain));
            }

            if (pending != null)
            {
                _pending.AddOrUpdate(context, pending);
            }
        }

        /// <summary>
        /// The entities keep the plain values after the save, unchanged
        /// </summary>
        private void Restore(DbContext context)
        {
            if (context == null || !_pending.TryGetValue(context, out var pending))
            {
                return;
            }

            _pending.Remove(context);
            foreach (var item in pending)
            {
                var wasModified = item.Property.IsModified;
                var unchanged = item.Property.EntityEntry.State == EntityState.Unchanged;

                item.Property.CurrentValue = item.Plain;
                if (unchanged || !wasModified)
                {
                    item.Property.OriginalValue = item.Plain;
                    item.Property.IsModified = false;
                }
            }
        }

        private static bool IsPasswordProperty(DbContext context, PropertyInstance property)
        {
            var template = property.This2PropertyTemplateNavigation;
            if (template != null)
            {
                return template.This2PropertyType == (long)PropertyTemplateType.Password;
            }

            if (PasswordTemplates.TryGetValue(property.This2PropertyTemplate, out var isPassword))
            {
                return isPassword;
            }

            var type = context.Set<PropertyTemplate>().AsNoTracking()
                .Where(a => a.ObjId == property.This2PropertyTemplate)
                .Select(a => (long?)a.This2PropertyType)
                .FirstOrDefault();

            if (type == null)
            {
                // a template added in the same save: decided by its tracked entry
                var tracked = context.ChangeTracker.Entries<PropertyTemplate>().FirstOrDefault(a => a.Entity.ObjId == property.This2PropertyTemplate);
                return tracked?.Entity.This2PropertyType == (long)PropertyTemplateType.Password;
            }

            isPassword = type == (long)PropertyTemplateType.Password;
            PasswordTemplates[property.This2PropertyTemplate] = isPassword;
            return isPassword;
        }

        #endregion

        /// <summary>
        /// Encrypts the plain secrets of an existing database (startup migration)
        /// </summary>
        /// <returns>the number of encrypted values</returns>
        public static int EncryptExisting(AutomaticaContext context)
        {
            var count = 0;

            var passwordTemplates = context.PropertyTemplates.AsNoTracking()
                .Where(a => a.This2PropertyType == (long)PropertyTemplateType.Password)
                .Select(a => a.ObjId)
                .ToList();

            // the materialization decrypts: a protected value loads as plain text and is re-encrypted only if it is marked modified.
            // Plain values are detected with a raw read of the stored value.
            foreach (var templates in passwordTemplates.Chunk(100))
            {
                var stored = context.PropertyInstances.AsNoTracking()
                    .Where(a => templates.Contains(a.This2PropertyTemplate) && a.ValueString != null && a.ValueString != "" && !a.ValueString.StartsWith(SecretProtector.Prefix))
                    .Select(a => a.ObjId)
                    .ToList();

                foreach (var ids in stored.Chunk(100))
                {
                    foreach (var property in context.PropertyInstances.Where(a => ids.Contains(a.ObjId)).ToList())
                    {
                        context.Entry(property).Property(a => a.ValueString).IsModified = true;
                        count++;
                    }
                }
            }

            var settingKeys = SecretSettingKeys.ToList();
            var settings = context.Settings
                .Where(a => (a.Type == (long)PropertyTemplateType.Password || settingKeys.Contains(a.ValueKey)) &&
                            a.ValueText != null && a.ValueText != "" && !a.ValueText.StartsWith(SecretProtector.Prefix))
                .ToList();
            foreach (var setting in settings)
            {
                context.Entry(setting).Property(a => a.ValueText).IsModified = true;
                count++;
            }

            if (count > 0)
            {
                context.SaveChanges();
            }

            return count;
        }
    }
}
