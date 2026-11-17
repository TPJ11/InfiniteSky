using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Values;

namespace InfiniteSky.Domain.Items;

/// <summary>
/// Preserves the optional/variant fields found in original Mixed inventory and storage objects.
/// </summary>
public sealed record InventoryItem
{
    /// <summary>
    /// Gets the item definition identifier.
    /// </summary>
    public required ItemDefinitionId ItemId { get; init; }

    /// <summary>
    /// Gets the optional inventory/storage grid position.
    /// </summary>
    public GridPosition? Position { get; init; }

    /// <summary>
    /// Gets an optional stack amount or pet-capacity value, as supplied by the original record.
    /// </summary>
    public long? Amount { get; init; }

    /// <summary>
    /// Gets the optional enchant value.
    /// </summary>
    public int? Enchant { get; init; }

    /// <summary>
    /// Gets the optional combine value.
    /// </summary>
    public int? Combine { get; init; }

    /// <summary>
    /// Gets the optional pet or bottle activity/capacity value.
    /// </summary>
    public long? Activity { get; init; }

    /// <summary>
    /// Gets the optional pet growth value.
    /// </summary>
    public long? Growth { get; init; }
}
