namespace Nawly.Shared.Common.Pagination;

public class PaginationResponse<TData>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public IReadOnlyList<TData> Data { get; set; }

    public PaginationResponse(int pageIndex, int pageSize, int totalCount, IReadOnlyList<TData> data)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
        TotalCount = totalCount;
        Data = data;
    }
}
