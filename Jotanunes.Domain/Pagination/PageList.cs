namespace Jotanunes.Domain.Pagination;

public class PageList<T>(List<T> items, int count, int pageNumber, int pageSize)
{
    public List<T> Items { get; } = items;
    public int CurrentPage { get; } = pageNumber;
    public int TotalPages { get; } = (int)Math.Ceiling(count / (double)pageSize);
    public int PageSize { get; } = pageSize;
    public int TotalCount { get; } = count;
}
