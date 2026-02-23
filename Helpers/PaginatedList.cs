using Microsoft.EntityFrameworkCore;

namespace SchoolEduERP.Helpers;

/// <summary>
/// Generic paginated list: wraps a query, applies Skip/Take in the DB and carries paging metadata.
/// </summary>
public class PaginatedList<T> : List<T>
{
    public int PageIndex { get; }
    public int PageSize  { get; }
    public int TotalCount { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage     => PageIndex < TotalPages;

    private PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
    {
        PageIndex  = pageIndex;
        PageSize   = pageSize;
        TotalCount = count;
        AddRange(items);
    }

    /// <summary>Create from an IQueryable (executes two DB calls: Count + Skip/Take).</summary>
    public static async Task<PaginatedList<T>> CreateAsync(
        IQueryable<T> source, int pageIndex, int pageSize)
    {
        var count = await source.CountAsync();
        var items = await source
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return new PaginatedList<T>(items, count, pageIndex, pageSize);
    }

    /// <summary>Create from an already-materialised list (no extra DB hit).</summary>
    public static PaginatedList<T> CreateFromList(
        IEnumerable<T> source, int pageIndex, int pageSize)
    {
        var list  = source.ToList();
        var items = list.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
        return new PaginatedList<T>(items, list.Count, pageIndex, pageSize);
    }
}
