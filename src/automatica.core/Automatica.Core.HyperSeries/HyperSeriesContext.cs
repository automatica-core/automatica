using Automatica.Core.HyperSeries.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.HyperSeries
{
    public class HyperSeriesContext : DbContext
    {
        private readonly IConfiguration? _config;
        public DbSet<RecordValue> RecordValues { get; set; } = default!;
        public DbSet<HourByHourAggregatedRecordValue> HourByHourAggregatedValues { get; set; } = default!;
        public DbSet<DayByDayAggregatedRecordValue> DayByDayAggregatedValues { get; set; } = default!;
        public DbSet<WeekByWeekAggregatedRecordValue> WeekByWeekAggregatedValues { get; set; } = default!;
        public DbSet<MonthByMonthAggregatedRecordValue> MonthByMonthAggregatedValues { get; set; } = default!;
        public DbSet<YearByYearAggregatedRecordValue> YearByYearAggregatedValues { get; set; } = default!;

        public HyperSeriesContext()
        {

        }

        public HyperSeriesContext(IConfiguration config)
        {
            _config = config;
        }
        public async Task AddRecordValue(RecordValue recordValue)
        {
            await Database.ExecuteSqlRawAsync(
                "INSERT INTO \"RecordValues\" (\"Timestamp\", \"TrendId\", \"NodeInstanceId\", \"Value\") VALUES ({0}, {1}, {2}, {3})",
                recordValue.Timestamp, recordValue.TrendId, recordValue.NodeInstanceId, recordValue.Value);

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                if (_config != null)
                {
                    var hyperSeriesConString =
                        $"Server={_config["db:hyperSeriesHost"]};User Id={_config["db:hyperSeriesUser"]};Password={_config["db:hyperSeriesPassword"]};Database={_config["db:hyperSeriesDatabase"]};Port={_config["db:hyperSeriesPort"]}";

                    if (string.IsNullOrEmpty(_config["db:hyperSeriesHost"]))
                    {
                        hyperSeriesConString = _config.GetConnectionString($"HyperSeriesConnection");
                    }

                    optionsBuilder.UseNpgsql(connectionString: hyperSeriesConString);
                }
                else
                {
                    //only for ef tool to migrate db!!!
                    optionsBuilder.UseNpgsql(
                        connectionString: "Server=localhost;User Id=postgres;Password=password;Database=postgres;");
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HourByHourAggregatedRecordValue>(entity =>
            {
                AddAggregatedProperty(entity);
                entity.ToTable("values_hour_by_hour");
            });
            modelBuilder.Entity<DayByDayAggregatedRecordValue>(entity =>
            {
                AddAggregatedProperty(entity);
                entity.ToTable("values_day_by_day");
            });
            modelBuilder.Entity<WeekByWeekAggregatedRecordValue>(entity =>
            {
                AddAggregatedProperty(entity);
                entity.ToTable("values_week_by_week");
            });
            modelBuilder.Entity<MonthByMonthAggregatedRecordValue>(entity =>
            {
                AddAggregatedProperty(entity);
                entity.ToTable("values_month_by_month");
            });
            modelBuilder.Entity<YearByYearAggregatedRecordValue>(entity =>
            {
                AddAggregatedProperty(entity);
                entity.ToTable("values_year_by_year");
            });
        }

        private void AddAggregatedProperty<T>(EntityTypeBuilder<T> entity) where T : AggregatedRecordValue
        {
            entity.Property<double>("AverageValue")
                .HasColumnType("double precision")
                .HasColumnName("avgvalue");

            entity.Property<int>("Count")
                .HasColumnType("integer")
                .HasColumnName("countvalue");

            entity.Property<double>("DifferenceValue")
                .HasColumnType("double precision")
                .HasColumnName("diffvalue");

            entity.Property<double>("MaxValue")
                .HasColumnType("double precision")
                .HasColumnName("maxvalue");

            entity.Property<double>("MinValue")
                .HasColumnType("double precision")
                .HasColumnName("minvalue");

            entity.Property<Guid>("NodeInstanceId")
                .HasColumnType("uuid")
                .HasColumnName("nodeinstanceid");

            entity.Property<DateTimeOffset>("Timestamp")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("time");

        }
    }
}