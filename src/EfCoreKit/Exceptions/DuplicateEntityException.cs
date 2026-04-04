namespace EfCoreKit.Exceptions;

/// <summary>
/// Thrown when an entity violates a unique constraint.
/// Wrap <see cref="Microsoft.EntityFrameworkCore.DbUpdateException"/> with this
/// to give callers a clean, typed exception for duplicate key scenarios.
/// </summary>
public class DuplicateEntityException : EfCoreException
{
    /// <summary>Gets the name of the entity type that caused the conflict.</summary>
    public string EntityName { get; }

    /// <summary>Gets the name of the field that has a duplicate value, or <c>null</c> if unknown.</summary>
    public string? FieldName { get; }

    /// <summary>Gets the duplicate value, or <c>null</c> if unknown.</summary>
    public object? FieldValue { get; }

    /// <summary>
    /// Initialises a new <see cref="DuplicateEntityException"/> with entity, field, and value information.
    /// </summary>
    /// <param name="entityName">The entity type name (e.g. <c>"Customer"</c>).</param>
    /// <param name="fieldName">The unique field name (e.g. <c>"Email"</c>). Optional.</param>
    /// <param name="fieldValue">The conflicting value. Optional.</param>
    public DuplicateEntityException(string entityName, string? fieldName = null, object? fieldValue = null)
        : base(BuildMessage(entityName, fieldName, fieldValue))
    {
        EntityName = entityName;
        FieldName  = fieldName;
        FieldValue = fieldValue;
    }

    private static string BuildMessage(string entityName, string? fieldName, object? fieldValue)
    {
        if (fieldName is null)
            return $"A {entityName} with the same unique key already exists.";

        return fieldValue is null
            ? $"A {entityName} with the same '{fieldName}' already exists."
            : $"A {entityName} with {fieldName} = '{fieldValue}' already exists.";
    }
}
