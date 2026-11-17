using InfiniteSky.Domain.Identifiers;

namespace InfiniteSky.Domain.Items;

/// <summary>
/// Represents an equipped pet and its source activity/growth values.
/// </summary>
public sealed record PetEquipment
{
    /// <summary>
    /// Gets the item definition identifier.
    /// </summary>
    public required ItemDefinitionId ItemId { get; init; }

    /// <summary>
    /// Gets the source activity value.
    /// </summary>
    public long Activity { get; init; }

    /// <summary>
    /// Gets the source growth value.
    /// </summary>
    public long Growth { get; init; }
}
