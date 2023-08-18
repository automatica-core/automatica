using Microsoft.EntityFrameworkCore.Migrations;


namespace Automatica.Core.HyperSeries.Migrations
{
    /// <inheritdoc />
    public partial class AddAggregations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AddMaterializedView(migrationBuilder, "values_day_by_day", "1 day");
            AddMaterializedView(migrationBuilder, "values_hour_by_hour", "1 hour");
            AddMaterializedView(migrationBuilder, "values_week_by_week", "1 week");
            AddMaterializedView(migrationBuilder, "values_month_by_month", "1 month");
            AddMaterializedView(migrationBuilder, "values_year_by_year", "1 year");
        }

        private void AddMaterializedView(MigrationBuilder migrationBuilder, string name, string timeBucket)
        {
            migrationBuilder.Sql(
                $"CREATE MATERIALIZED VIEW {name}(time, avgValue, diffValue, maxValue, minValue, countValue, nodeInstanceId) " +
                "with (timescaledb.continuous) as " +
                $"SELECT time_bucket('{timeBucket}', \"Timestamp\") AS time, " +
                "(avg(\"Value\")) AS avgValue, " +
                "(last(\"Value\", \"Timestamp\") - first(\"Value\", \"Timestamp\")) AS diffValue, " +
                "max(\"Value\") AS maxValue, " +
                "min(\"Value\") AS minValue, " +
                "count(*) as \"count\"," +
                "\"NodeInstanceId\" as \"nodeInstanceId\"" +
                "FROM \"RecordValues\" GROUP BY 1, \"NodeInstanceId\"", true);

            migrationBuilder.Sql(
                $"SELECT add_continuous_aggregate_policy('{name}', " +
                "start_offset => NULL, " +
                "end_offset => INTERVAL '1 hour', " +
                "schedule_interval => INTERVAL '1 hour');", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
