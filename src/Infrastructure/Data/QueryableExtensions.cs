using Microsoft.EntityFrameworkCore;
using ProductManagement.Application.DTOs;

namespace ProductManagement.Infrastructure.Data;

internal static class QueryableExtensions
{
    /// <summary>Runs a count and a single page query. The query must already be ordered so paging is stable.</summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> orderedQuery, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var totalCount = await orderedQuery.CountAsync(cancellationToken);

        // Computed in long so an absurd page number yields an empty page instead of an overflow.
        var skip = (int)Math.Min((long)(pageNumber - 1) * pageSize, int.MaxValue);
        var items = await orderedQuery.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<T>(items, pageNumber, pageSize, totalCount);
    }
}
