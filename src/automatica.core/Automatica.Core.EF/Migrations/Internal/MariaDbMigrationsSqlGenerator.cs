using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using MySql.EntityFrameworkCore.Infrastructure;

namespace Automatica.Core.EF.Migrations.Internal
{
    /// <summary>
    /// Wraps Oracle's migrations SQL generator.
    /// The old migrations (EF Core 3) have no column type and their target model does not always contain the column:
    /// Oracle then crashes with a NullReferenceException, EF itself falls back to the CLR type. The type is resolved here first.
    /// </summary>
    internal class MariaDbMigrationsSqlGenerator : IMigrationsSqlGenerator
    {
        private const string OracleGeneratorType = "MySql.EntityFrameworkCore.Migrations.MySQLMigrationsSqlGenerator";

        private readonly IMigrationsSqlGenerator _inner;
        private readonly IRelationalTypeMappingSource _typeMappingSource;

        public MariaDbMigrationsSqlGenerator(IServiceProvider serviceProvider, IRelationalTypeMappingSource typeMappingSource)
        {
            _typeMappingSource = typeMappingSource;

            // the class is internal in MySql.EntityFrameworkCore
            var innerType = typeof(MySQLDbContextOptionsBuilder).Assembly.GetType(OracleGeneratorType, throwOnError: true);
            _inner = (IMigrationsSqlGenerator)ActivatorUtilities.CreateInstance(serviceProvider, innerType);
        }

        public IReadOnlyList<MigrationCommand> Generate(IReadOnlyList<MigrationOperation> operations, IModel model = null, MigrationsSqlGenerationOptions options = MigrationsSqlGenerationOptions.Default)
        {
            operations = operations.Select(DropForeignKeyIfExists).ToList();

            if (model != null)
            {
                // migrations generated with the SQLite provider have SQLite store types in their target model (Guid/DateTime -> TEXT,
                // bool -> INTEGER), Oracle would create TEXT primary keys and indexes. Those columns get the MariaDB type of their CLR type
                foreach (var (schema, table, name, column) in operations.SelectMany(NamedColumns))
                {
                    if (column != null && column.ColumnType == null && HasSqliteStoreType(schema, table, name, model))
                    {
                        ResolveColumnType(column);
                    }
                }
            }

            if (model == null || operations.All(a => !HasColumnMissingInModel(a, model)))
            {
                return _inner.Generate(operations, model, options);
            }

            // Oracle reads type and charset of every column from the model and crashes on a column the model does not know.
            // Those operations get their type from the CLR type and are generated without the model, the others keep it
            var commands = new List<MigrationCommand>();
            foreach (var operation in operations)
            {
                if (HasColumnMissingInModel(operation, model))
                {
                    foreach (var column in Columns(operation))
                    {
                        ResolveColumnType(column);
                    }
                    commands.AddRange(_inner.Generate(new[] { operation }, null, options));
                }
                else
                {
                    commands.AddRange(_inner.Generate(new[] { operation }, model, options));
                }
            }
            return commands;
        }

        /// <summary>
        /// Databases created by older versions do not always have the foreign keys of the model (e.g. NodeInstance_ibfk_2 and
        /// PropertyInstance_ibfk_1 are missing in installations that started on Pomelo), a plain DROP FOREIGN KEY fails the whole
        /// migration there. MariaDB supports IF EXISTS, the following AddForeignKey recreates the key either way.
        /// </summary>
        internal static MigrationOperation DropForeignKeyIfExists(MigrationOperation operation)
        {
            if (operation is not DropForeignKeyOperation drop)
            {
                return operation;
            }

            var table = drop.Schema == null ? $"`{drop.Table}`" : $"`{drop.Schema}`.`{drop.Table}`";
            return new SqlOperation { Sql = $"ALTER TABLE {table} DROP FOREIGN KEY IF EXISTS `{drop.Name}`;" };
        }

        private static IEnumerable<ColumnOperation> Columns(MigrationOperation operation)
        {
            switch (operation)
            {
                case AddColumnOperation addColumn:
                    yield return addColumn;
                    break;
                case AlterColumnOperation alterColumn:
                    yield return alterColumn;
                    yield return alterColumn.OldColumn;
                    break;
                case CreateTableOperation createTable:
                    foreach (var column in createTable.Columns)
                    {
                        yield return column;
                    }
                    break;
            }
        }

        private static readonly HashSet<string> SqliteStoreTypes = new(StringComparer.Ordinal) { "TEXT", "INTEGER", "REAL", "BLOB", "NUMERIC" };

        /// <summary>
        /// The SQLite generator writes its type affinities in upper case, the MySQL providers write lower case types ("longtext", "int")
        /// </summary>
        private static bool HasSqliteStoreType(string schema, string table, string name, IModel model)
        {
            var storeType = model.GetRelationalModel().FindTable(table, schema)?.FindColumn(name)?.StoreType;
            return storeType != null && SqliteStoreTypes.Contains(storeType);
        }

        private static IEnumerable<(string Schema, string Table, string Name, ColumnOperation Column)> NamedColumns(MigrationOperation operation)
        {
            switch (operation)
            {
                case AddColumnOperation addColumn:
                    yield return (addColumn.Schema, addColumn.Table, addColumn.Name, addColumn);
                    break;
                case AlterColumnOperation alterColumn:
                    yield return (alterColumn.Schema, alterColumn.Table, alterColumn.Name, alterColumn);
                    yield return (alterColumn.Schema, alterColumn.Table, alterColumn.Name, alterColumn.OldColumn);
                    break;
                case CreateTableOperation createTable:
                    foreach (var column in createTable.Columns)
                    {
                        yield return (createTable.Schema, createTable.Name, column.Name, column);
                    }
                    break;
            }
        }

        private static bool HasColumnMissingInModel(MigrationOperation operation, IModel model)
        {
            var (schema, table, columns) = operation switch
            {
                AddColumnOperation a => (a.Schema, a.Table, new[] { a.Name }),
                AlterColumnOperation a => (a.Schema, a.Table, new[] { a.Name }),
                CreateTableOperation a => (a.Schema, a.Name, a.Columns.Select(c => c.Name).ToArray()),
                _ => (null, null, null)
            };
            if (table == null)
            {
                return false;
            }

            var relationalTable = model.GetRelationalModel().FindTable(table, schema);
            return columns.Any(c => relationalTable?.FindColumn(c) == null);
        }

        private void ResolveColumnType(ColumnOperation operation)
        {
            if (operation == null || operation.ColumnType != null || operation.ClrType == null)
            {
                return;
            }

            operation.ColumnType = _typeMappingSource.FindMapping(operation.ClrType, null, false, operation.IsUnicode, operation.MaxLength,
                operation.IsRowVersion, operation.IsFixedLength, operation.Precision, operation.Scale)?.StoreType;
        }
    }
}
