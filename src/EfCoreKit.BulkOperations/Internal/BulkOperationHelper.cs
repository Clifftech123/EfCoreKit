using System.Data;
using Microsoft.EntityFrameworkCore;

namespace EfCoreKit.BulkOperations.Internal;

/// <summary>
/// Shared utility methods used by database-specific <see cref="Abstractions.IBulkExecutor"/> implementations.
/// </summary>
internal static class BulkOperationHelper
{
    /// <summary>
    /// Creates a <see cref="DataTable"/> from a collection of entities using the EF Core model metadata.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The entities to convert.</param>
    /// <param name="tableMapping">The resolved table and column mapping metadata.</param>
    /// <returns>A <see cref="DataTable"/> populated with the entity data.</returns>
    public static DataTable CreateDataTable<T>(IList<T> entities, TableMapping tableMapping) where T : class
    {
        // TODO: Build DataTable columns from tableMapping.Columns
        // TODO: Populate rows from entity property values using compiled accessors
        throw new NotImplementedException();
    }

    /// <summary>
    /// Generates a SQL statement to create a temporary table matching the entity's schema.
    /// </summary>
    /// <param name="tableMapping">The table mapping metadata.</param>
    /// <param name="tempTableName">The temporary table name to use.</param>
    /// <returns>A SQL CREATE TABLE statement for the temp table.</returns>
    public static string BuildCreateTempTableSql(TableMapping tableMapping, string tempTableName)
    {
        // TODO: Generate CREATE TABLE #tempTableName with columns matching tableMapping
        throw new NotImplementedException();
    }

    /// <summary>
    /// Generates a SQL statement to drop a temporary table.
    /// </summary>
    /// <param name="tempTableName">The temporary table name to drop.</param>
    /// <returns>A SQL DROP TABLE statement.</returns>
    public static string BuildDropTempTableSql(string tempTableName)
    {
        // TODO: Generate DROP TABLE IF EXISTS statement
        throw new NotImplementedException();
    }

    /// <summary>
    /// Generates a MERGE SQL statement for upsert operations.
    /// </summary>
    /// <param name="tableMapping">The table mapping metadata.</param>
    /// <param name="tempTableName">The source temporary table name.</param>
    /// <param name="updateByProperties">The columns to match on. Defaults to primary key columns.</param>
    /// <returns>A SQL MERGE statement.</returns>
    public static string BuildMergeSql(
        TableMapping tableMapping,
        string tempTableName,
        IList<string>? updateByProperties = null)
    {
        // TODO: Generate MERGE INTO target USING tempTable ON (key match)
        // WHEN MATCHED THEN UPDATE WHEN NOT MATCHED THEN INSERT
        throw new NotImplementedException();
    }

    /// <summary>
    /// Generates a DELETE with JOIN SQL statement for bulk delete operations.
    /// </summary>
    /// <param name="tableMapping">The table mapping metadata.</param>
    /// <param name="tempTableName">The source temporary table containing the keys to delete.</param>
    /// <returns>A SQL DELETE statement with JOIN.</returns>
    public static string BuildDeleteWithJoinSql(TableMapping tableMapping, string tempTableName)
    {
        // TODO: Generate DELETE target FROM target INNER JOIN tempTable ON (key match)
        throw new NotImplementedException();
    }

    /// <summary>
    /// Batches a collection of entities into smaller chunks.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The entities to batch.</param>
    /// <param name="batchSize">The maximum number of entities per batch.</param>
    /// <returns>An enumerable of batches.</returns>
    public static IEnumerable<IList<T>> BatchEntities<T>(IList<T> entities, int batchSize) where T : class
    {
        for (var i = 0; i < entities.Count; i += batchSize)
        {
            yield return entities.Skip(i).Take(batchSize).ToList();
        }
    }

    /// <summary>
    /// Resolves the <see cref="TableMapping"/> for an entity type from the given context.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="context">The <see cref="DbContext"/>.</param>
    /// <returns>The resolved <see cref="TableMapping"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the entity type is not found in the model.</exception>
    public static TableMapping GetTableMapping<T>(DbContext context) where T : class
    {
        var entityType = context.Model.FindEntityType(typeof(T))
            ?? throw new InvalidOperationException(
                $"Entity type '{typeof(T).Name}' is not part of the model for context '{context.GetType().Name}'.");

        return TableMapping.Create(entityType);
    }
}
