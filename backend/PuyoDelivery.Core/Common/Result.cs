namespace PuyoDelivery.Core.Common;

public class Result
{
    public bool IsSuccess { get; set; }
    public string? Error { get; set; }
    public string? Code { get; set; }

    public static Result Success() => new() { IsSuccess = true };
    public static Result Failure(string error, string? code = null) => new() { IsSuccess = false, Error = error, Code = code };
}

public class Result<T> : Result
{
    public T? Data { get; set; }

    public static Result<T> Success(T data) => new() { IsSuccess = true, Data = data };
    public static new Result<T> Failure(string error, string? code = null) => new() { IsSuccess = false, Error = error, Code = code };
}
