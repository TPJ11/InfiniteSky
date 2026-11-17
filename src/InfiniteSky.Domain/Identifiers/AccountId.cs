namespace InfiniteSky.Domain.Identifiers;

/// <summary>
/// Identifies an account stored by the original server.
/// </summary>
public readonly record struct AccountId
{
    /// <summary>
    /// Initializes the identifier.
    /// </summary>
    public AccountId(long value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        Value = value;
    }

    /// <summary>
    /// Gets the source account counter value.
    /// </summary>
    public long Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
