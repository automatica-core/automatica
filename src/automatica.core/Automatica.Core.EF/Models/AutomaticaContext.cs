using Automatica.Core.EF.Helper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Reflection;
using Automatica.Core.EF.Models.Areas;
using Automatica.Core.EF.Models.Categories;
using System;
using Automatica.Core.Model.Models.User;
using Automatica.Core.EF.Models.Trendings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Automatica.Core.EF.Models
{
    public class AutomaticaContext : DbContext
    {
        private readonly bool _extendedLogs;
        public virtual DbSet<BoardInterface> BoardInterfaces { get; set; }
        public virtual DbSet<BoardType> BoardTypes { get; set; }
        public virtual DbSet<InterfaceType> InterfaceTypes { get; set; }
        public virtual DbSet<Link> Links { get; set; }
        public virtual DbSet<NodeDataType> NodeDataTypes { get; set; }
        public virtual DbSet<NodeInstance> NodeInstances { get; set; }
        public virtual DbSet<NodeInstance2RulePage> NodeInstance2RulePages { get; set; }
        public virtual DbSet<NodeTemplate> NodeTemplates { get; set; }
        public virtual DbSet<PropertyInstance> PropertyInstances { get; set; }
        public virtual DbSet<PropertyTemplate> PropertyTemplates { get; set; }
        public virtual DbSet<PropertyType> PropertyTypes { get; set; }
        public virtual DbSet<RuleInstance> RuleInstances { get; set; }
        public virtual DbSet<RuleInterfaceDirection> RuleInterfaceDirections { get; set; }
        public virtual DbSet<RuleInterfaceInstance> RuleInterfaceInstances { get; set; }
        public virtual DbSet<RuleInterfaceTemplate> RuleInterfaceTemplates { get; set; }
        public virtual DbSet<RulePage> RulePages { get; set; }
        public virtual DbSet<RulePageType> RulePageTypes { get; set; }
        public virtual DbSet<RuleTemplate> RuleTemplates { get; set; }
        public virtual DbSet<Setting> Settings { get; set; }

        public virtual DbSet<VisuPageType> VisuPageTypes { get; set; }
        public virtual DbSet<VisuPage> VisuPages { get; set; }
        public virtual DbSet<VisuObjectInstance> VisuObjectInstances { get; set; }
        public virtual DbSet<VisuObjectTemplate> VisuObjectTemplates { get; set; }

        public virtual DbSet<PropertyTemplateConstraint> PropertyTemplateConstraints { get; set; }
        public virtual DbSet<PropertyTemplateConstraintData> PropertyTemplateConstraintData { get; set; }
        public virtual DbSet<VersionInformation> VersionInformations { get; set; }


        public virtual DbSet<AreaType> AreaTypes { get; set; }
        public virtual DbSet<AreaTemplate> AreaTemplates { get; set; }
        public virtual DbSet<AreaInstance> AreaInstances { get; set; }

        public virtual DbSet<CategoryGroup> CategoryGroups { get; set; }
        public virtual DbSet<CategoryInstance> CategoryInstances { get; set; }


        public virtual DbSet<Role> Roles { get; set; }
        public virtual DbSet<User> Users { get; set; }

        public virtual DbSet<Priviledge> Priviledges { get; set; }
        public virtual DbSet<User2Group> User2Groups { get; set; }
        public virtual DbSet<UserGroup> UserGroups { get; set; }
        public virtual DbSet<Priviledge2Role> Priviledge2Roles { get; set; }
        public virtual DbSet<User2Role> User2Roles { get; set; }
        public virtual DbSet<UserGroup2Role> UserGroup2Roles { get; set; }
        public virtual DbSet<Plugin> Plugins { get; set; }

        public virtual DbSet<Trending> Trendings { get; set; }
        public virtual DbSet<Slave> Slaves { get; set; }

        public IConfiguration Configuration { get; }

        public AutomaticaContext(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public AutomaticaContext(IConfiguration configuration, bool extendedLogs) : this(configuration)
        {
            _extendedLogs = extendedLogs;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var logger = new DatabaseLoggerFactory();
                var dbType = Configuration.GetConnectionString("AutomaticaDatabaseType");
                var envDbType = Environment.GetEnvironmentVariable("DATABASE_TYPE");
                var loggerInstance = NullLogger.Instance; // logger.CreateLogger("database");

                string useDbType = envDbType;

                if (string.IsNullOrEmpty(envDbType))
                {
                    useDbType = dbType;
                    loggerInstance.LogWarning($"Using databasetype from appsettings, environment variable \"DATABASE_TYPE\" is not set");
                }

                if (string.IsNullOrEmpty(dbType))
                {
                    loggerInstance.LogError($"No DatabaseType is set! Using sqlite database driver!");
                    useDbType = "sqlite";
                    dbType = useDbType;
                }

                

                switch(useDbType.ToLower())
                {
                    case "sqlite":
                        ConfigureSqLiteDatabase(optionsBuilder, loggerInstance);
                        break;
                    case "mariadb":
                        ConfigureMariaDatabase(optionsBuilder, loggerInstance);
                        break;
                    case "memory":
                        optionsBuilder.UseInMemoryDatabase("automatica");
                        loggerInstance.LogInformation($"Using MemoryDatabase provider...");
                        break;
                    default:
                        loggerInstance.LogCritical($"No or invalid database provider configured {dbType.ToLower()}\nSupported are sqlite, mariadb, memory");
                        break;
                }


                if (_extendedLogs || !string.IsNullOrEmpty($"DATABASE_LOGS"))
                {
                    optionsBuilder.UseLoggerFactory(logger);
                }
            }

            optionsBuilder.EnableSensitiveDataLogging();
        }

        private void ConfigureMariaDatabase(DbContextOptionsBuilder optionsBuilder, ILogger logger)
        {
            logger.LogInformation($"Using MariaDB database engine...");

            var mariaDbConString = $"Server={Environment.GetEnvironmentVariable("MARIADB_HOST")};User Id={Environment.GetEnvironmentVariable("MARIADB_USER")};Password={Environment.GetEnvironmentVariable("MARIADB_PASSWORD")};Database=automatica";

            if(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MARIADB_HOST")))
            {
                mariaDbConString = Configuration.GetConnectionString($"AutomaticaDatabaseMaria");
                logger.LogWarning($"Using connection string from appsettings.json because to environment variable is defined");
            }

            optionsBuilder.UseMySql(mariaDbConString);
        }

        private void ConfigureSqLiteDatabase(DbContextOptionsBuilder optionsBuilder, ILogger logger)
        {

            logger.LogInformation($"Using SQLite database engine...");
            string conString = Configuration.GetConnectionString("AutomaticaDatabaseSqlite");
            var sqliteConBuilder = new SqliteConnectionStringBuilder(conString);

            var fi = new FileInfo(Assembly.GetEntryAssembly().Location);
            var dbFile = Path.Combine(fi.DirectoryName, sqliteConBuilder.DataSource);
            var dbInitFile = Path.Combine(fi.DirectoryName, DatabaseConstants.DatabaseInitName);

            if (File.Exists(dbInitFile) && !File.Exists(dbFile))
            {
                File.Copy(dbInitFile, dbFile);
            }

            optionsBuilder.UseSqlite(conString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Setting>(entity =>
            {
                entity.HasKey(e => e.ObjId);
                entity.Property(e => e.ObjId).ValueGeneratedOnAdd();

                entity.Property(e => e.ValueKey).IsRequired();
                entity.Property(e => e.ValueText);
                entity.Property(e => e.ValueInt);
                entity.Property(e => e.ValueDouble);
                entity.Property(e => e.Type);
                entity.Property(e => e.IsVisible).HasDefaultValue(false);

                entity.Property(e => e.Group).HasDefaultValue("System");
                entity.Ignore(e => e.TypeInfo);
            });

            modelBuilder.Entity<BoardInterface>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.ObjId);

                entity.Property(e => e.ObjId)

                    .ValueGeneratedNever();

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1024).HasDefaultValue("");

                entity.Property(e => e.Meta)
                    .IsRequired()
                    .HasMaxLength(1024);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(1024);

                entity.Property(e => e.This2BoardType);

                entity.Property(e => e.This2InterfaceType);

                entity.HasOne(d => d.This2BoardTypeNavigation)
                    .WithMany(p => p.BoardInterface)
                    .HasForeignKey(d => d.This2BoardType)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("BoardInterface_ibfk_1");

                entity.HasOne(d => d.This2InterfaceTypeNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.This2InterfaceType)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("BoardInterface_ibfk_2");
            });

            modelBuilder.Entity<BoardType>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.Type);

                entity.Property(e => e.Type)

                    .ValueGeneratedNever();

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1024).HasDefaultValue("");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(1024);
            });

            modelBuilder.Entity<InterfaceType>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.Type);

                entity.Property(e => e.Type)

                    .ValueGeneratedNever();

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1024).HasDefaultValue("");

                entity.Property(e => e.MaxChilds);

                entity.Property(e => e.IsDriverInterface).HasDefaultValue(false);
                entity.Property(e => e.MaxInstances);
                entity.Property(e => e.CanProvideBoardType).HasDefaultValue(false);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(1024);
            });

            modelBuilder.Entity<Link>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.ObjId);

                entity.Property(e => e.ObjId)

                    .ValueGeneratedNever();

                entity.Property(e => e.IsDeleted).HasDefaultValue(false);

                entity.Property(e => e.This2NodeInstance2RulePageInput);

                entity.Property(e => e.This2NodeInstance2RulePageOutput);

                entity.Property(e => e.This2RuleInterfaceInstanceInput);

                entity.Property(e => e.This2RuleInterfaceInstanceOutput);

                entity.Property(e => e.This2RulePage);

                entity.HasOne(d => d.This2RulePageNavigation)
                    .WithMany(p => p.Link)
                    .HasForeignKey(d => d.This2RulePage)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("Link_ibfk_1");

                entity.HasOne(d => d.This2NodeInstance2RulePageInputNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.This2NodeInstance2RulePageInput)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("Link_ibfk_2");


                entity.HasOne(d => d.This2RuleInterfaceInstanceInputNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.This2RuleInterfaceInstanceInput)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("Link_ibfk_3");


                entity.HasOne(d => d.This2RuleInterfaceInstanceOutputNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.This2RuleInterfaceInstanceOutput)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("Link_ibfk_4");


                entity.HasOne(d => d.This2NodeInstance2RulePageOutputNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.This2NodeInstance2RulePageOutput)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("Link_ibfk_5");
            });

            modelBuilder.Entity<NodeDataType>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.Type);

                entity.Property(e => e.Type)
                    .ValueGeneratedNever();

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1024).HasDefaultValue("");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(1024);
            });



            modelBuilder.Entity<NodeInstance>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.ObjId);


                entity.Property(e => e.ObjId)

                    .ValueGeneratedNever();

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1024).HasDefaultValue("");

                entity.Property(e => e.IsDeleted).HasDefaultValue(false);

                entity.Property(e => e.IsReadable).HasDefaultValue(false);

                entity.Property(e => e.IsWriteable).HasDefaultValue(false);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(1024);

                entity.Property(e => e.This2NodeTemplate);

                entity.Property(e => e.This2ParentNodeInstance);

                entity.Property(e => e.UseInVisu).HasDefaultValue(false);
                entity.Property(e => e.This2AreaInstance);
                entity.Property(e => e.This2CategoryInstance);

                entity.HasOne(e => e.This2AreaInstanceNavigation)
                    .WithMany()
                    .HasForeignKey(a => a.This2AreaInstance)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("NodeInstance_This2AreaInstance");

                entity.HasOne(e => e.This2CategoryInstanceNavigation)
                    .WithMany()
                    .HasForeignKey(a => a.This2CategoryInstance)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("NodeInstance_This2CategoryInstance");

                entity.HasOne(d => d.This2NodeTemplateNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.This2NodeTemplate)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("NodeInstance_ibfk_2");

                entity.HasOne(d => d.This2ParentNodeInstanceNavigation)
                    .WithMany(p => p.InverseThis2ParentNodeInstanceNavigation)
                    .HasForeignKey(d => d.This2ParentNodeInstance)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("NodeInstance_ibfk_3");


                entity.Property(a => a.This2UserGroup);
                entity.Property(a => a.IsFavorite);
                entity.Property(a => a.Rating);

                entity.Property(a => a.StateColorValueFalse).HasDefaultValue("rgba(0, 0, 0, 1)");
                entity.Property(a => a.StateColorValueTrue).HasDefaultValue("rgba(0, 0, 0, 1)");
                entity.Property(a => a.StateTextValueFalse).HasDefaultValue("0");
                entity.Property(a => a.StateTextValueTrue).HasDefaultValue("1");
                entity.Property(a => a.VisuName);


                entity.HasOne(d => d.This2UserGroupNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.This2UserGroup)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("NodeInstance_This2UserGroup");

                entity.HasOne(d => d.This2SlaveNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.This2Slave)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("NodeInstance_This2Slave");
            });

            modelBuilder.Entity<NodeInstance2RulePage>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.ObjId);

                entity.Property(e => e.ObjId)

                    .ValueGeneratedNever();

                entity.Property(e => e.IsDeleted).HasDefaultValue(false);

                entity.Property(e => e.This2NodeInstance);

                entity.Property(e => e.This2RulePage);

                entity.HasOne(d => d.This2NodeInstanceNavigation)
                    .WithMany(p => p.NodeInstance2RulePage)
                    .HasForeignKey(d => d.This2NodeInstance)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("NodeInstance2RulePage_ibfk_2");

                entity.HasOne(d => d.This2RulePageNavigation)
                    .WithMany(p => p.NodeInstance2RulePage)
                    .HasForeignKey(d => d.This2RulePage)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("NodeInstance2RulePage_ibfk_1");
            });

            modelBuilder.Entity<NodeTemplate>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.ObjId);

                entity.Property(e => e.ObjId)

                    .ValueGeneratedNever();

                entity.Property(e => e.DefaultCreated).HasDefaultValue(false);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1024).HasDefaultValue("");

                entity.Property(e => e.IsAdapterInterface).HasDefaultValue(false);

                entity.Property(e => e.IsDeleteable).HasDefaultValue(false);

                entity.Property(e => e.IsReadable).HasDefaultValue(false);

                entity.Property(e => e.IsReadableFixed).HasDefaultValue(false);

                entity.Property(e => e.IsWriteable).HasDefaultValue(false);

                entity.Property(e => e.IsWriteableFixed).HasDefaultValue(false);

                entity.Property(e => e.Key)
                    .IsRequired()
                    .HasMaxLength(1024);

                entity.Property(e => e.MaxInstances);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(1024);

                entity.Property(e => e.NeedsInterface2InterfacesType);

                entity.Property(e => e.ProvidesInterface2InterfaceType);

                entity.Property(e => e.This2NodeDataType);
                entity.Property(e => e.NameMeta);

                entity.Property(e => e.This2DefaultMobileVisuTemplate).HasDefaultValue(new Guid("16780dfd-887a-4a0a-9b2a-4d62ccc32c93"));

                entity.HasOne(d => d.THis2DefaultMobileVisuTemplateNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.This2DefaultMobileVisuTemplate)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("NodeTemplate_DefaultVisuMobileTemplate");

                entity.HasOne(d => d.NeedsInterface2InterfacesTypeNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.NeedsInterface2InterfacesType)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("NodeTemplate_ibfk_1");

                entity.HasOne(d => d.ProvidesInterface2InterfaceTypeNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.ProvidesInterface2InterfaceType)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("NodeTemplate_ibfk_2");

                entity.HasOne(d => d.This2NodeDataTypeNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.This2NodeDataType)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("NodeTemplate_ibfk_3");
            });

            modelBuilder.Entity<PropertyInstance>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.ObjId);

                entity.Property(e => e.ObjId)

                   .ValueGeneratedNever();

                entity.Property(e => e.IsDeleted).HasDefaultValue(false);

                entity.Property(e => e.This2NodeInstance);
                entity.Property(e => e.This2VisuObjectInstance);

                entity.Property(e => e.This2PropertyTemplate);

                entity.Property(e => e.ValueBool).HasDefaultValue(false);

                entity.Property(e => e.ValueInt);
                entity.Property(e => e.ValueNodeInstance);
                entity.Property(e => e.ValueRulePage);
                entity.Property(e => e.ValueVisuPage);
                entity.Property(e => e.ValueAreaInstance);
                entity.Property(e => e.ValueCategoryInstance);
                entity.Property(e => e.ValueLong);

                entity.Property(e => e.ValueString);

                entity.HasOne(d => d.This2NodeInstanceNavigation)
                    .WithMany(p => p.PropertyInstance)
                    .HasForeignKey(d => d.This2NodeInstance)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("PropertyInstance_ibfk_2");

                entity.HasOne(e => e.ValueNodeInstanceNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.ValueNodeInstance)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("PropertyInstance_NodeInstance_ValueNodeInstance");

                entity.HasOne(e => e.ValueRulePageNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.ValueRulePage)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("PropertyInstance_RulePage_ValueRulePage");

                entity.HasOne(e => e.ValueVisuPageNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.ValueVisuPage)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("PropertyInstance_VisuPage_ValueVisuPage");

                entity.HasOne(d => d.This2VisuObjectInstanceNavigation)
                    .WithMany(p => p.PropertyInstance)
                    .HasForeignKey(d => d.This2VisuObjectInstance)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("PropertyInstance_VisuObjectInstance_ibfk_3");

                entity.HasOne(d => d.ValueAreaInstanceNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.ValueAreaInstance)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("PropertyInstance_AreaInstance_ValueAreaInstance");

                entity.HasOne(d => d.ValueCategoryInstanceNavigation)
                 .WithMany()
                 .HasForeignKey(d => d.ValueCategoryInstance)
                 .OnDelete(DeleteBehavior.Cascade)
                 .HasConstraintName("PropertyInstance_CategoryInstance_ValueCategoryInstance");

                entity.HasOne(d => d.ValueSlaveNavigation)
                 .WithMany()
                 .HasForeignKey(d => d.ValueSlave)
                 .OnDelete(DeleteBehavior.ClientSetNull)
                 .HasConstraintName("PropertyInstance_Slave_ValueSlave");


                entity.HasOne(d => d.This2PropertyTemplateNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.This2PropertyTemplate)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("PropertyInstance_ibfk_1");
            });

            modelBuilder.Entity<PropertyTemplate>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.ObjId);

                entity.Property(e => e.ObjId)

                    .ValueGeneratedNever();

                entity.Property(e => e.DefaultValue).HasMaxLength(1024);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1024).HasDefaultValue("");

                entity.Property(e => e.Group)
                    .IsRequired()
                    .HasMaxLength(1024);

                entity.Property(e => e.GroupOrder)
                    .HasDefaultValueSql("1");

                entity.Property(e => e.IsReadonly).HasDefaultValue(false);

                entity.Property(e => e.IsVisible).HasDefaultValue(false);

                entity.Property(e => e.Key)
                    .IsRequired()
                    .HasMaxLength(1024);

                entity.Property(e => e.Meta).IsRequired();

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(1024);

                entity.Property(e => e.Order)
                    .HasDefaultValueSql("1");

                entity.Property(e => e.This2NodeTemplate).HasDefaultValue();
                entity.Property(e => e.This2VisuObjectTemplate).HasDefaultValue();

                entity.Property(e => e.This2PropertyType);


                entity.HasOne(d => d.This2NodeTemplateNavigation)
                    .WithMany(p => p.PropertyTemplate)
                    .HasForeignKey(d => d.This2NodeTemplate)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("PropertyTemplate_ibfk_2");

                entity.HasOne(d => d.This2VisuObjectTemplateNavigation)
                    .WithMany(p => p.PropertyTemplate)
                    .HasForeignKey(d => d.This2VisuObjectTemplate)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("PropertyTemplate_ibfk_3");

                entity.HasOne(d => d.This2PropertyTypeNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.This2PropertyType)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("PropertyTemplate_ibfk_1");
            });

            modelBuilder.Entity<PropertyTemplateConstraintData>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.ObjId);
                entity.Property(e => e.ObjId);
                entity.Property(e => e.Factor).HasDefaultValue(1.0);
                entity.Property(e => e.Offset).HasDefaultValue(0.0);
                entity.Property(e => e.ConditionType).IsRequired();


                entity.Property(e => e.PropertyKey);
                entity.Property(e => e.This2PropertyTemplateConstraint);

                entity.HasOne(d => d.This2PropertyTemplateConstraintNavigation)
                    .WithMany(p => p.ConstraintData)
                    .HasForeignKey(d => d.This2PropertyTemplateConstraint)
                    .HasConstraintName("This2PropertyTemplateConstraintNavigation_PropertyTemplate_Navigation");
            });

            modelBuilder.Entity<PropertyTemplateConstraint>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.ObjId);
                entity.Property(e => e.ObjId);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(1024);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1024).HasDefaultValue("");

                entity.Property(e => e.This2PropertyTemplate);

                entity.Property(e => e.ConstraintType).IsRequired();
                entity.Property(e => e.ConstraintLevel).IsRequired();

                entity.HasOne(d => d.This2PropertyTemplateNavigation)
                    .WithMany(p => p.Constraints)
                    .HasForeignKey(d => d.This2PropertyTemplate)
                    .HasConstraintName("PropertyTemplateConstraint_PropertyTemplate_Navigation");
            });

            modelBuilder.Entity<PropertyType>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.Type);

                entity.Property(e => e.Type)
                    .ValueGeneratedNever();

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1024).HasDefaultValue("");

                entity.Property(e => e.Meta).HasMaxLength(1024);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(1024);
            });

            modelBuilder.Entity<RuleInstance>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.ObjId);

                entity.Property(e => e.ObjId)

                    .ValueGeneratedNever();

                entity.Property(e => e.Description).HasMaxLength(1024).HasDefaultValue("");

                entity.Property(e => e.IsDeleted).HasDefaultValue(false);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(1024);

                entity.Property(e => e.This2RulePage);

                entity.Property(e => e.This2RuleTemplate);

                entity.Property(e => e.UseInVisu).HasDefaultValue(false);
                entity.Property(e => e.This2AreaInstance);
                entity.Property(e => e.This2CategoryInstance);

                entity.HasOne(e => e.This2AreaInstanceNavigation)
                    .WithMany()
                    .HasForeignKey(a => a.This2AreaInstance)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("RuleInstance_This2AreaInstance");

                entity.HasOne(e => e.This2CategoryInstanceNavigation)
                    .WithMany()
                    .HasForeignKey(a => a.This2CategoryInstance)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("RuleInstance_This2CategoryInstance");

                entity.HasOne(d => d.This2RulePageNavigation)
                    .WithMany(p => p.RuleInstance)
                    .HasForeignKey(d => d.This2RulePage)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("RuleInstance_ibfk_2");

                entity.HasOne(d => d.This2RuleTemplateNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.This2RuleTemplate)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("RuleInstance_ibfk_1");

                entity.Property(a => a.This2UserGroup);
                entity.HasOne(d => d.This2UserGroupNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.This2UserGroup)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("RuleInstance_This2UserGroup");
            });

            modelBuilder.Entity<RuleInterfaceDirection>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.ObjId);

                entity.Property(e => e.ObjId)
                    .ValueGeneratedNever();

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1024).HasDefaultValue("");

                entity.Property(e => e.Key)
                    .IsRequired()
                    .HasMaxLength(1024);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(1024);
            });

            modelBuilder.Entity<RuleInterfaceInstance>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.ObjId);

                entity.Property(e => e.ObjId)

                    .ValueGeneratedNever();

                entity.Property(e => e.IsDeleted).HasDefaultValue(false);

                entity.Property(e => e.This2RuleInstance);

                entity.Property(e => e.This2RuleInterfaceTemplate);

                entity.Property(e => e.ValueDouble);
                entity.Property(e => e.ValueInteger);
                entity.Property(e => e.ValueString);

                entity.HasOne(d => d.This2RuleInstanceNavigation)
                    .WithMany(p => p.RuleInterfaceInstance)
                    .HasForeignKey(d => d.This2RuleInstance)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("RuleInterfaceInstance_ibfk_1");

                entity.HasOne(d => d.This2RuleInterfaceTemplateNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.This2RuleInterfaceTemplate)
                    .HasConstraintName("RuleInterfaceInstance_ibfk_2");
            });

            modelBuilder.Entity<RuleInterfaceTemplate>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.ObjId);

                entity.Property(e => e.ObjId)

                    .ValueGeneratedNever();

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1024).HasDefaultValue("");

                entity.Property(e => e.MaxLinks);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(1024);

                entity.Property(e => e.This2RuleInterfaceDirection);

                entity.Property(e => e.This2RuleTemplate);

                entity.Property(e => e.ParameterDataType).HasConversion(type => (int)type, i => (RuleInterfaceParameterDataType)i);
                entity.Property(e => e.DefaultValue);

                entity.HasOne(d => d.This2RuleInterfaceDirectionNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.This2RuleInterfaceDirection)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("RuleInterfaceTemplate_ibfk_2");

                entity.HasOne(d => d.This2RuleTemplateNavigation)
                    .WithMany(p => p.RuleInterfaceTemplate)
                    .HasForeignKey(d => d.This2RuleTemplate)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("RuleInterfaceTemplate_ibfk_1");
            });

            modelBuilder.Entity<RulePage>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.ObjId);

                entity.Property(e => e.ObjId)

                    .ValueGeneratedNever();

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1024).HasDefaultValue("");

                entity.Property(e => e.IsDeleted).HasDefaultValue(false);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(1024);

                entity.Property(e => e.This2RulePageType);

                entity.HasOne(d => d.This2RulePageTypeNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.This2RulePageType)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("RulePage_ibfk_1");
            });

            modelBuilder.Entity<RulePageType>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.ObjId);

                entity.Property(e => e.ObjId)
                    .ValueGeneratedNever();

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1024).HasDefaultValue("");

                entity.Property(e => e.Key)
                    .IsRequired()
                    .HasMaxLength(1024);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(1024);
            });

            modelBuilder.Entity<RuleTemplate>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.ObjId);

                entity.Property(e => e.ObjId)

                    .ValueGeneratedNever();

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1024).HasDefaultValue("");

                entity.Property(e => e.Group)
                    .IsRequired()
                    .HasMaxLength(1024);

                entity.Property(e => e.Key)
                    .IsRequired()
                    .HasMaxLength(1024);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(1024);


                entity.Property(e => e.This2DefaultMobileVisuTemplate).HasDefaultValue(new Guid("16780dfd-887a-4a0a-9b2a-4d62ccc32c93"));

                entity.HasOne(d => d.This2DefaultMobileVisuTemplateNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.This2DefaultMobileVisuTemplate)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("RuleTemplate_DefaultVisuMobileTemplate");
            });


            modelBuilder.Entity<VisuPage>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.ObjId);

                entity.HasIndex(a => a.ObjId);

                entity.Property(e => e.ObjId)

                    .ValueGeneratedNever();

                entity.Property(e => e.DefaultPage);

                entity.HasIndex(e => e.This2VisuPageType)
                    .HasName("This2VisuPageType");

                entity.Property(a => a.Height).HasDefaultValue(4).IsRequired();
                entity.Property(a => a.Width).HasDefaultValue(6).IsRequired();

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(1024);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1024).HasDefaultValue("");

                entity.Property(e => e.This2VisuPageType);

                entity.HasOne(d => d.This2VisuPageTypeNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.This2VisuPageType)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("VisuPage_ibfk_1");

                entity.Property(a => a.This2UserGroup);
                entity.Property(a => a.IsFavorite);
                entity.Property(a => a.Rating);


                entity.HasOne(d => d.This2UserGroupNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.This2UserGroup)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("VisuPage_This2UserGroup");
            });
            modelBuilder.Entity<VisuObjectInstance>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.ObjId);

                entity.Property(e => e.ObjId)

                    .ValueGeneratedNever();

                entity.Property(e => e.Description).HasMaxLength(1024).HasDefaultValue("");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(1024);

                entity.Property(e => e.This2VisuPage);

                entity.Property(e => e.This2VisuObjectTemplate);

                entity.HasOne(d => d.This2VisuObjectTemplateNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.This2VisuObjectTemplate)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("VisuObjectInstance_ibfk_1");

                entity.HasOne(d => d.This2VisuPageNavigation)
                    .WithMany(p => p.VisuObjectInstances)
                    .HasForeignKey(d => d.This2VisuPage)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("VisuObjectInstance_ibfk_2");

                entity.Property(a => a.This2UserGroup);
                entity.Property(a => a.IsFavorite);
                entity.Property(a => a.Rating);


                entity.HasOne(d => d.This2UserGroupNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.This2UserGroup)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("VisuObjectInstance_This2UserGroup");
            });


            modelBuilder.Entity<VisuObjectTemplate>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.ObjId);

                entity.Property(e => e.ObjId)
                .ValueGeneratedNever();

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(1024);

                entity.Property(e => e.Key)
                    .IsRequired()
                    .HasMaxLength(1024);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1024).HasDefaultValue("");

                entity.Property(e => e.Group)
                    .IsRequired()
                    .HasMaxLength(1024);

                entity.Property(e => e.IsVisibleForUser).HasDefaultValue(true);

                entity.Property(e => e.This2VisuPageType).IsRequired();

                entity.HasOne(d => d.This2VisuPageTypeNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.This2VisuPageType)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_VisuObjectTemplates_VisuPageTypes_VisuPageTypeObjId");
            });
            modelBuilder.Entity<VisuPageType>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.ObjId);

                entity.Property(e => e.ObjId)
                    .ValueGeneratedNever();

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1024).HasDefaultValue("");

                entity.Property(e => e.Key)
                    .IsRequired()
                    .HasMaxLength(1024);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(1024);
            });

            modelBuilder.Entity<AreaType>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.ObjId);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(1024);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1024).HasDefaultValue("");
            });

            modelBuilder.Entity<AreaTemplate>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.ObjId);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(1024);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1024).HasDefaultValue("");


                entity.Property(e => e.This2AreaType).IsRequired();
                entity.Property(e => e.ProvidesThis2AreayType).IsRequired();
                entity.Property(e => e.NeedsThis2AreaType).IsRequired();
                entity.Property(e => e.IsDeleteable).HasDefaultValue(true).IsRequired();

                entity.Property(e => e.Icon).IsRequired();


                entity.HasOne(d => d.This2AreaTypeNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.This2AreaType)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_AreaTemplate_AreaType");

                entity.HasOne(d => d.ProvidesThis2AreayTypeNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.ProvidesThis2AreayType)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_AreaTemplate_ProvidesThis2AreayTypeNavigation");

                entity.HasOne(d => d.NeedsThis2AreaTypeNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.NeedsThis2AreaType)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_AreaTemplate_NeedsThis2AreaTypeNavigation");

            });

            modelBuilder.Entity<AreaInstance>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.ObjId);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(1024);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1024).HasDefaultValue("");


                entity.Property(e => e.This2AreaTemplate).IsRequired();
                entity.Property(e => e.This2Parent);
                entity.Property(e => e.Icon).IsRequired();

                entity.HasOne(d => d.This2ParentNavigation)
                    .WithMany(d => d.InverseThis2ParentNavigation)
                    .HasForeignKey(d => d.This2Parent)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_AreaInstance_This2ParentNavigation");

                entity.HasOne(d => d.This2AreaTemplateNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.This2AreaTemplate)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_AreaInstance_This2AreaTemplateNavigation");

                entity.Property(a => a.This2UserGroup);
                entity.Property(a => a.IsFavorite);
                entity.Property(a => a.Rating);


                entity.HasOne(d => d.This2UserGroupNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.This2UserGroup)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_AreaInstance_This2UserGroup");

            });

            modelBuilder.Entity<CategoryGroup>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.ObjId);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(1024);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1024).HasDefaultValue("");
            });

            modelBuilder.Entity<CategoryInstance>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.ObjId);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(1024);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1024).HasDefaultValue("");

                entity.Property(e => e.Color).IsRequired().HasMaxLength(128).HasDefaultValue("rgba(255, 255, 255, 1)");
                entity.Property(e => e.Icon).IsRequired().HasMaxLength(128);

                entity.Property(e => e.IsFavorite).HasDefaultValue(false);
                entity.Property(e => e.IsDeleteable).HasDefaultValue(true);

                entity.HasOne(d => d.This2CategoryGroupNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.This2CategoryGroup)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_CategoryInstance_This2CategoryGroupNavigation");

                entity.Property(a => a.This2UserGroup);
                entity.Property(a => a.Rating);

                entity.HasOne(d => d.This2UserGroupNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.This2UserGroup)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_CategoryInstance_This2UserGroup");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.ObjId);

                entity.Property(e => e.UserName).IsRequired().HasMaxLength(128);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(128);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(128);
                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1024).HasDefaultValue("");


                entity.Property(e => e.Salt).IsRequired().HasMaxLength(1024);
                entity.Property(e => e.Password).IsRequired().HasMaxLength(1024);

                entity.Ignore(e => e.TypeInfo);
                entity.Ignore(e => e.PasswordConfirm);
                entity.Ignore(e => e.Token);
            });
            modelBuilder.Entity<UserGroup>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.ObjId);

                entity.Property(e => e.Name).IsRequired().HasMaxLength(128);
                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1024).HasDefaultValue("");
            });

            modelBuilder.Entity<User2Group>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(a => new { a.This2User, a.This2UserGroup });

                entity.HasOne(a => a.This2UserNavigation)
                    .WithMany(a => a.InverseThis2UserGroups).
                    HasForeignKey(a => a.This2User);

                entity.HasOne(a => a.This2UserGroupNavigation)
                    .WithMany(a => a.InverseThis2Users).
                    HasForeignKey(a => a.This2UserGroup);
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.ObjId);

                entity.Property(e => e.Name).IsRequired().HasMaxLength(128);
                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1024).HasDefaultValue("");
                entity.Property(e => e.Key).IsRequired().HasMaxLength(128);
                entity.Property(e => e.IsDeleteable).HasDefaultValue(false);
            });

            modelBuilder.Entity<Priviledge>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(e => e.ObjId);

                entity.Property(e => e.Name).IsRequired().HasMaxLength(128);
                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1024).HasDefaultValue("");
                entity.Property(e => e.Key).IsRequired().HasMaxLength(128);
            });

            modelBuilder.Entity<Priviledge2Role>(entity =>
            {
                entity.HasKey(a => a.This2Priviledge);
                entity.HasKey(a => a.This2Role);

                entity.HasOne(a => a.This2PriviledgeNavigation)
                    .WithMany(a => a.InverseThis2Roles).
                    HasForeignKey(a => a.This2Priviledge);

                entity.HasOne(a => a.This2RoleNavigation)
                    .WithMany(a => a.InverseThis2Priviledges).
                    HasForeignKey(a => a.This2Role);
            });



            modelBuilder.Entity<User2Role>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(a => new { a.This2User, a.This2Role });


                entity.HasOne(a => a.This2UserNavigation)
                    .WithMany(a => a.InverseThis2Roles).
                    HasForeignKey(a => a.This2User);

                entity.HasOne(a => a.This2RoleNavigation)
                    .WithMany().
                    HasForeignKey(a => a.This2Role);
            });

            modelBuilder.Entity<UserGroup2Role>(entity =>
            {
                entity.Ignore(e => e.TypeInfo);
                entity.HasKey(a => new { a.This2UserGroup, a.This2Role });

                entity.HasOne(a => a.This2UserGroupNavigation)
                    .WithMany(a => a.InverseThis2Roles).
                    HasForeignKey(a => a.This2UserGroup);

                entity.HasOne(a => a.This2RoleNavigation)
                    .WithMany().
                    HasForeignKey(a => a.This2Role);

            });

            modelBuilder.Entity<Plugin>(entity =>
            {
                entity.HasKey(a => new { a.ObjId });
                entity.Property(e => e.ObjId).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<VersionInformation>(entity =>
            {
                entity.HasKey(e => e.ObjId);

                entity.Property(e => e.ObjId)
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.DriverGuid)

                    .HasMaxLength(36);

                entity.Property(e => e.RuleGuid)

                    .HasMaxLength(36);

                entity.Property(e => e.Name)
                    .HasMaxLength(1024);

                entity.Property(e => e.Version)
                    .IsRequired()
                    .HasMaxLength(1024);
            });

            modelBuilder.Entity<Trending>(entity =>
            {
                entity.HasKey(e => e.ObjId);
                entity.Property(e => e.ObjId).ValueGeneratedOnAdd();
                entity.Property(e => e.Source).HasMaxLength(1024);

                entity.HasOne(d => d.This2NodeInstanceNavigation)
                  .WithMany()
                  .HasForeignKey(d => d.This2NodeInstance)
                  .OnDelete(DeleteBehavior.Cascade);

            });

            modelBuilder.Entity<Slave>(entity =>
            {
                entity.HasKey(e => e.ObjId);

                entity.Property(e => e.ObjId).ValueGeneratedOnAdd();
                entity.Property(e => e.Name);
                entity.Property(e => e.Description);

                entity.Property(e => e.ClientId);
                entity.Property(e => e.ClientKey);
            });
        }
    }
}
