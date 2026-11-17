using InfiniteSky.Domain.Identifiers;

namespace InfiniteSky.Domain.Items;

/// <summary>
/// Represents a non-pet equipped item.
/// </summary>
public sealed record EquipmentItem
{
    /// <summary>
    /// Gets the item definition identifier.
    /// </summary>
    public required ItemDefinitionId ItemId { get; init; }

    /// <summary>
    /// Gets the source enchant value.
    /// </summary>
    public int Enchant { get; init; }

    /// <summary>
    /// Gets the source combine value.
    /// </summary>
    public int Combine { get; init; }

    /// <summary>
    /// Gets optional capacity data used by bottle-like equipment.
    /// </summary>
    public long? Capacity { get; init; }
}
