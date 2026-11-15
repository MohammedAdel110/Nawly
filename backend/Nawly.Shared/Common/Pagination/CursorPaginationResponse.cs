namespace Nawly.Shared.Common.Pagination;

/// <summary>
/// Keyset/cursor pagination envelope for orders and notifications (SRS §7.1).
/// </summary>
public sealed class CursorPaginationResponse<TData>
{
    public IReadOnlyList<TData> Data { get; }
    public string? NextCursor { get; }
    public int? TotalCount { get; }

    public CursorPaginationResponse(IReadOnlyList<TData> data, string? nextCursor, int? totalCount = null)
    {
        Data = data;
        NextCursor = nextCursor;
        TotalCount = totalCount;
    }
}
