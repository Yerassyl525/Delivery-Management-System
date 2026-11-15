namespace DeliveryFinalProject;

public sealed class Result<T>
{
    private readonly T? _value;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }

    public T Value => IsFailure
        ? throw new InvalidOperationException(Error)
        : _value!;

    private Result(T value)
    {
        _value = value;
        IsSuccess = true;
        Error = null;
    }

    private Result(string error)
    {
        _value = default;
        IsSuccess = false;
        Error = error;
    }

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("Error message is required.", nameof(error));
        return new Result<T>(error);
    }

    public TResult Match<TResult>(Func<T, TResult> success, Func<string, TResult> failure) => this switch
    {
        { IsSuccess: true } => success(Value),
        { IsFailure: true } => failure(Error!),
        _ => throw new InvalidOperationException("Unknown result state.")
    };

    public Result<TResult> Map<TResult>(Func<T, TResult> map)
    {
        ArgumentNullException.ThrowIfNull(map);
        return IsFailure
            ? Result<TResult>.Failure(Error!)
            : Result<TResult>.Success(map(Value));
    }

    public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> bind)
    {
        ArgumentNullException.ThrowIfNull(bind);
        return IsFailure
            ? Result<TResult>.Failure(Error!)
            : bind(Value);
    }
}
