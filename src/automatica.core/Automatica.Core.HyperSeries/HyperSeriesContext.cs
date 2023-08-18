using Automatica.Core.HyperSeries.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.HyperSeries
{
    public class HyperSeriesContext : DbContext
    {
        private readonly IConfiguration? _config;
        public DbSet<RecordValue> RecordValues { get; set; } = default!;

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
                "INSERT INTO \"RecordValues\" (\"Timestamp\", \"TrendId\", \"NodeInstanceId\", \"Value\") VALUES ({0}, {1}, {2}, {3})", recordValue.Timestamp, recordValue.TrendId, recordValue.NodeInstanceId, recordValue.Value);

        }

        //public IQueryable<IntervalResult> GetWeeklyResults(DateTime value)
        //{
        //    if (value.Kind != DateTimeKind.Utc)
        //    {
        //        // Read this and cry https://www.npgsql.org/doc/types/datetime.html
        //        throw new ArgumentException("DateTime.Kind must be of UTC to convert to timestamp with time zone");
        //    }

        //    return FromExpression(() => GetWeeklyResults(value));
        //}

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
                    optionsBuilder.UseNpgsql(connectionString: "Server=localhost;User Id=postgres;Password=password;Database=postgres;");
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // shouldn't be used since we have a method
           // modelBuilder
                //.HasDbFunction(typeof(StocksDbContext).GetMethod(nameof(GetWeeklyResults), new[] { typeof(DateTime) })!)
                // map to entity and don't worry about tables
                // mapping to a table in the snapshot 
               // .HasName("get_weekly_results")
                //.IsBuiltIn(false);
        }
    }
}