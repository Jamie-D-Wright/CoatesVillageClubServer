namespace VillageClub.Contracts.Models;

/// <summary>
/// Generic paginated response wrapper
/// </summary>
/// <typeparam name="T">Type of items in the result set</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Collection of items for the current page
    /// </summary>
    public IEnumerable<T> Items { get; set; } = Array.Empty<T>();

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => Page > 1;
}
