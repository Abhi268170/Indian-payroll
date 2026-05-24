namespace Payroll.Application.DTOs;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Total,
    int Page,
    int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)Total / PageSize);
}

public sealed record PaginationParams(int Page = 1, int PageSize = 25)
{
    // Clamp to safe bounds. PageSize options exposed in UI: 25 / 50 / 100.
    public int SkipCount => Math.Max(0, (Math.Max(1, Page) - 1) * NormalizedSize);
    public int TakeCount => NormalizedSize;
    public int NormalizedSize => Math.Clamp(PageSize, 1, 200);
    public int NormalizedPage => Math.Max(1, Page);
}
