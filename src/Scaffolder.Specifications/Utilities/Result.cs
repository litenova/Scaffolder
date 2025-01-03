namespace Scaffolder.Specifications.Utilities;

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail with an error message.
/// </summary>
/// <typeparam name="T">The type of the value in case of success</typeparam>
public readonly record struct Result<T>
{
    private readonly T? _value;
    private readonly string? _error;

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess => _error is null;

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the value if the operation was successful.
    /// Throws InvalidOperationException if the operation failed.
    /// </summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot access value of failed result. Error: {_error}");

    /// <summary>
    /// Gets the error message if the operation failed.
    /// Throws InvalidOperationException if the operation was successful.
    /// </summary>
    public string Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access error of successful result");

    private Result(T value)
    {
        _value = value;
        _error = null;
    }

    private Result(string error)
    {
        _value = default;
        _error = error;
    }

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(string error) => new(error);
}

/// <summary>
/// Provides static methods for creating Results.
/// </summary>
public static class Result
{
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);
}