namespace InfiniteSky.Domain.Identifiers;

/// <summary>
/// Identifies a skill definition from the original skill information file.
/// </summary>
public readonly record struct SkillDefinitionId
{
    /// <summary>
    /// Initializes the identifier.
    /// </summary>
    public SkillDefinitionId(uint value) => Value = value;

    /// <summary>
    /// Gets the unsigned definition identifier.
    /// </summary>
    public uint Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
