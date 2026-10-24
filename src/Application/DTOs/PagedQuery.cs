namespace ProductManagement.Application.DTOs;

/// <summary>Paging parameters accepted by every collection endpoint.</summary>
public sealed record PagedQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    /// <summary>1-based page number.</summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>Number of items per page (1 to <see cref="MaxPageSize"/>).</summary>
    public int PageSize { get; init; } = DefaultPageSize;
}
