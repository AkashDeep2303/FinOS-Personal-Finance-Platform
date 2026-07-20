namespace FinOS.Common.Models;

/// <summary>
/// Base query parameters for paginated API requests.
/// Provides consistent pagination semantics across all FinOS endpoints.
/// </summary>
public class PagedQuery
{
    private int _page = 1;
    private int _pageSize = 10;

    /// <summary>
    /// Page number (1-based). Values below 1 are clamped to 1.
    /// </summary>
    public int Page
    {
        get => _page;
        set => _page = Math.Max(1, value);
    }

    /// <summary>
    /// Alias for <see cref="Page"/> — some codebases prefer PageNumber.
    /// </summary>
    public int PageNumber
    {
        get => Page;
        set => Page = value;
    }

    /// <summary>
    /// Number of items per page. Clamped between 1 and <see cref="MaxPageSize"/> (default 100).
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Clamp(value, 1, MaxPageSize);
    }

    /// <summary>
    /// Optional search term for filtering results.
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Alias for <see cref="Search"/> — some codebases prefer SearchTerm.
    /// </summary>
    public string? SearchTerm
    {
        get => Search;
        set => Search = value;
    }

    /// <summary>
    /// Field name to sort by.
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort direction — "asc" or "desc". Defaults to "asc".
    /// </summary>
    public string SortDirection { get; set; } = "asc";

    /// <summary>
    /// Maximum allowed page size. Override in derived classes if needed.
    /// </summary>
    protected virtual int MaxPageSize => 100;

    /// <summary>
    /// Calculates the number of rows to skip for the current page.
    /// Useful when building OFFSET … FETCH NEXT SQL queries.
    /// </summary>
    public int Skip => (Page - 1) * PageSize;

    /// <summary>
    /// Whether the sort direction is descending.
    /// </summary>
    public bool IsDescending => SortDirection?.Equals("desc", StringComparison.OrdinalIgnoreCase) == true;
}
