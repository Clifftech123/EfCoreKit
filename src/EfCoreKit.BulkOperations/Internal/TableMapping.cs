using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EfCoreKit.BulkOperations.Internal;

/// <summary>
/// Holds the resolved table and column mapping metadata for an entity type.
/// Used internally by bulk executors to map entity properties to database columns.
/// </summary>
internal sealed class TableMapping
{
    /// <summary>
    /// Gets the database schema name. <c>null</c> for the default schema.
    /// </summary>
    public string? Schema { get; }

    /// <summary>
    /// Gets the database table name.
    /// </summary>
    public string TableName { get; }

    /// <summary>
    /// Gets the fully qualified table name (e.g., <c>[dbo].[Customers]</c>).
    /// </summary>
    public string FullTableName => Schema is not null
        ? $"[{Schema}].[{TableName}]"
        : $"[{TableName}]";

    /// <summary>
    /// Gets the column mappings for each entity property.
    /// </summary>
    public IReadOnlyList<ColumnMapping> Columns { get; }

    /// <summary>
    /// Gets the primary key column names.
    /// </summary>
    public IReadOnlyList<string> PrimaryKeyColumns { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TableMapping"/> class.
    /// </summary>
    /// <param name="schema">The database schema name.</param>
    /// <param name="tableName">The database table name.</param>
    /// <param name="columns">The column mappings.</param>
    /// <param name="primaryKeyColumns">The primary key column names.</param>
    public TableMapping(
        string? schema,
        string tableName,
        IReadOnlyList<ColumnMapping> columns,
        IReadOnlyList<string> primaryKeyColumns)
    {
        Schema = schema;
        TableName = tableName;
        Columns = columns;
        PrimaryKeyColumns = primaryKeyColumns;
    }

    /// <summary>
    /// Creates a <see cref="TableMapping"/> from an EF Core entity type.
    /// </summary>
    /// <param name="entityType">The EF Core entity type metadata.</param>
    /// <returns>A <see cref="TableMapping"/> describing the entity's table structure.</returns>
    public static TableMapping Create(IEntityType entityType)
    {
        var tableName = entityType.GetTableName()
            ?? throw new InvalidOperationException(
                $"Entity type '{entityType.ClrType.Name}' is not mapped to a table.");
        var schema = entityType.GetSchema();
        var storeObject = StoreObjectIdentifier.Table(tableName, schema);

        var columns = entityType.GetProperties()
            .Select(p => new ColumnMapping(
                p.Name,
                p.GetColumnName(storeObject)
                    ?? throw new InvalidOperationException(
                        $"Property '{p.Name}' on '{entityType.ClrType.Name}' has no column mapping."),
                p.ClrType))
            .ToList();

        var primaryKey = entityType.FindPrimaryKey()
            ?? throw new InvalidOperationException(
                $"Entity type '{entityType.ClrType.Name}' does not have a primary key defined.");

        var pkColumns = primaryKey.Properties
            .Select(p => p.GetColumnName(storeObject)
                ?? throw new InvalidOperationException(
                    $"Primary key property '{p.Name}' on '{entityType.ClrType.Name}' has no column mapping."))
            .ToList();

        return new TableMapping(schema, tableName, columns, pkColumns);
    }
}

/// <summary>
/// Maps a single entity property to its database column.
/// </summary>
internal sealed class ColumnMapping
{
    /// <summary>
    /// Gets the CLR property name.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Gets the database column name.
    /// </summary>
    public string ColumnName { get; }

    /// <summary>
    /// Gets the CLR type of the property.
    /// </summary>
    public Type PropertyType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ColumnMapping"/> class.
    /// </summary>
    /// <param name="propertyName">The CLR property name.</param>
    /// <param name="columnName">The database column name.</param>
    /// <param name="propertyType">The CLR type of the property.</param>
    public ColumnMapping(string propertyName, string columnName, Type propertyType)
    {
        PropertyName = propertyName;
        ColumnName = columnName;
        PropertyType = propertyType;
    }
}
