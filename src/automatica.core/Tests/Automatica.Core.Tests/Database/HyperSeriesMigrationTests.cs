using System.Linq;
using Automatica.Core.HyperSeries;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Automatica.Core.Tests.Database
{
    /// <summary>
    /// Since EF Core 9 Migrate() throws PendingModelChangesWarning when the model differs from the last migration's snapshot, and
    /// HyperSeriesRepository then leaves the recorder deactivated: no history is written at all. This happened on servers from
    /// .NET 10 on (the aggregate views day/week/month/year were never in the snapshot); fixed by SyncAggregateViewSnapshot.
    /// </summary>
    public class HyperSeriesMigrationTests
    {
        [Fact]
        public void Model_HasNoChangesWithoutAMigration()
        {
            using var context = new HyperSeriesContext();

            Assert.False(context.Database.HasPendingModelChanges(),
                "The HyperSeries model changed without a migration: run 'dotnet ef migrations add <Name>' in Automatica.Core.HyperSeries.");
        }

        [Fact]
        public void Migrations_EndWithTheSnapshotSync()
        {
            using var context = new HyperSeriesContext();

            Assert.Equal("20261109131724_SyncAggregateViewSnapshot", context.Database.GetMigrations().Last());
        }
    }
}
