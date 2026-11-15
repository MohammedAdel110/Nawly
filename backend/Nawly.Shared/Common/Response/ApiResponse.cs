namespace Nawly.Shared.Common.Response;

public class ApiResponse<TData>
{
    public int StatusCode { get; set; }
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? TraceId { get; set; }
    public TData? Data { get; set; }
    public List<string>? Errors { get; set; } = [];

    public ApiResponse(TData data, string msg = "Success", int statusCode = 200)
    {
        Data = data;
        Message = msg;
        StatusCode = statusCode;
        IsSuccess = true;
    }

    public ApiResponse(
        string msg,
        int statusCode = 400,
        List<string>? errors = null,
        string? code = null,
        string? traceId = null)
    {
        IsSuccess = false;
        Message = msg;
        StatusCode = statusCode;
        Errors = errors ?? [];
        Code = code;
        TraceId = traceId;
    }
}
