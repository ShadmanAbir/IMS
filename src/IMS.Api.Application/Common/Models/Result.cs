namespace IMS.Api.Application.Common.Models;

/// <summary>
/// Represents the result of an operation
/// </summary>
public class Result
{
    protected Result(bool isSuccess, string? errorCode = null, string? errorMessage = null)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    public static Result Success() => new(true);
    public static Result Failure(string errorCode, string errorMessage) => new(false, errorCode, errorMessage);
}

/// <summary>
/// Represents the result of an operation that returns a value
/// </summary>
/// <typeparam name="T">The type of value returned</typeparam>
public class Result<T> : Result
{
    private readonly T? _value;

    protected Result(T value) : base(true)
    {
        _value = value;
    }

    protected Result(string errorCode, string errorMessage) : base(false, errorCode, errorMessage)
    {
        _value = default;
    }

    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("Cannot access value of a failed result");

    public static Result<T> Success(T value) => new(value);
    public static new Result<T> Failure(string errorCode, string errorMessage) => new(errorCode, errorMessage);
}