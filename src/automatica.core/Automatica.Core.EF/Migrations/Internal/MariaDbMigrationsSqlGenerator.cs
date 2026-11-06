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
