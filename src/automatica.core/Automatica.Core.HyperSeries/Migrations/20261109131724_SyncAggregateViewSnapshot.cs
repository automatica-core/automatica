using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Automatica.Core.HyperSeries.Migrations
{
    /// <inheritdoc />
    public partial class SyncAggregateViewSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nothing to create: values_day/week/month/year_by_year exist as TimescaleDB continuous aggregates since
            // AddAggregations (raw SQL), but the model snapshot only knew values_hour_by_hour. Since EF Core 9 Migrate()
            // throws PendingModelChangesWarning for that difference, and the recorder stayed deactivated. This migration
            // only brings the snapshot up to date with the model.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
