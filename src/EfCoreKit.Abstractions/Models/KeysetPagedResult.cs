namespace EfCoreKit.Abstractions.Models;

/// <summary>
/// Represents the result of a keyset (cursor-based) paginated query.
/// Keyset pagination is more efficient than offset pagination for large datasets
/// because it avoids scanning skipped rows.
/// </summary>
/// <typeparam name="T">The type of items in the result set.</typeparam>
public sealed class KeysetPagedResult<T>
{
    /// <summary>
    /// Gets the items on the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>
    /// Gets the cursor value for fetching the next page.
    /// <c>null</c> when there are no more pages.
    /// </summary>
    public string? NextCursor { get; }

    /// <summary>
    /// Gets the cursor value for fetching the previous page.
    /// <c>null</c> when on the first page.
    /// </summary>
    public string? PreviousCursor { get; }

    /// <summary>
    /// Gets a value indicating whether more results are available beyond the current page.
    /// </summary>
    public bool HasMore { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeysetPagedResult{T}"/> class.
    /// </summary>
    /// <param name="items">The items on the current page.</param>
    /// <param name="nextCursor">The cursor for the next page, or <c>null</c> if no more pages.</param>
    /// <param name="previousCursor">The cursor for the previous page, or <c>null</c> if on the first page.</param>
    /// <param name="hasMore">Whether more results are available.</param>
    public KeysetPagedResult(
        IList<T> items,
        string? nextCursor,
        string? previousCursor,
        bool hasMore)
    {
        Items = items.AsReadOnly();
        NextCursor = nextCursor;
        PreviousCursor = previousCursor;
        HasMore = hasMore;
    }
}
