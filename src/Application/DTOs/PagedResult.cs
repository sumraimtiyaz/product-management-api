namespace ProductManagement.Application.DTOs;

/// <summary>One page of a collection together with the information needed to navigate it.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public PagedResult<TOut> Map<TOut>(Func<T, TOut> selector) =>
        new(Items.Select(selector).ToList(), PageNumber, PageSize, TotalCount);
}
