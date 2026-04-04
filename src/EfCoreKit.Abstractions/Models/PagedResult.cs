namespace EfCoreKit.Abstractions.Models;

/// <summary>
/// Represents the result of an offset-based paginated query.
/// </summary>
/// <typeparam name="T">The type of items in the result set.</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>
    /// Gets the items on the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>
    /// Gets the total number of items across all pages.
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Gets the current page number (1-based).
    /// </summary>
    public int Page { get; }

    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages { get; }

    /// <summary>Gets a value indicating whether there is a previous page.</summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>Gets a value indicating whether there is a next page.</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>Gets a value indicating whether the result set is empty.</summary>
    public bool IsEmpty => Items.Count == 0;

    /// <summary>
    /// Gets the 1-based index of the first item on this page.
    /// Useful for "Showing X–Y of Z" labels. Returns 0 when the page is empty.
    /// </summary>
    public int From => IsEmpty ? 0 : (Page - 1) * PageSize + 1;

    /// <summary>
    /// Gets the 1-based index of the last item on this page.
    /// Returns 0 when the page is empty.
    /// </summary>
    public int To => IsEmpty ? 0 : From + Items.Count - 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="PagedResult{T}"/> class.
    /// </summary>
    /// <param name="items">The items on the current page.</param>
    /// <param name="totalCount">The total number of items across all pages.</param>
    /// <param name="page">The current page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    public PagedResult(IList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items.AsReadOnly();
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
    }

    /// <summary>
    /// Projects each item in the result to a different type while preserving pagination metadata.
    /// </summary>
    /// <typeparam name="TDestination">The target type to map to.</typeparam>
    /// <param name="mapper">A function that maps each item from <typeparamref name="T"/> to <typeparamref name="TDestination"/>.</param>
    /// <returns>A new <see cref="PagedResult{TDestination}"/> with mapped items.</returns>
    public PagedResult<TDestination> Map<TDestination>(Func<T, TDestination> mapper)
    {
        var mappedItems = Items.Select(mapper).ToList();
        return new PagedResult<TDestination>(mappedItems, TotalCount, Page, PageSize);
    }
}
