namespace DeliveryFinalProject;

public abstract record Option<T> where T : class
{
    private Option() { }

    public sealed record Some(T Value) : Option<T>;
    public sealed record None : Option<T>;

    public static Option<T> FromNullable(T? value) =>
        value is null ? new None() : new Some(value);

    public TResult Match<TResult>(Func<T, TResult> some, Func<TResult> none) => this switch
    {
        Some value => some(value.Value),
        None => none(),
        _ => throw new InvalidOperationException("Unknown option case.")
    };
}
