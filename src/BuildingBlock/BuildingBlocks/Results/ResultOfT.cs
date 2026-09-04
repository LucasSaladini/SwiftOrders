using BuildingBlocks.Results;

namespace ResultBlocks.Results;

public class ResultOfT<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public Error? Error { get; }

    private ResultOfT(T value)
    {
        IsSuccess = true;
        Value = value;
        Error = null;
    }

    private ResultOfT(Error error)
    {
        IsSuccess = false;
        Value = default;
        Error = error;
    }

    public static ResultOfT<T> Success(T value)
    {
        return new ResultOfT<T>(value);
    }

    public static ResultOfT<T> Failure(Error error)
    {
        return new ResultOfT<T>(error);
    }
}