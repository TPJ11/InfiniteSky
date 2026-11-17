using InfiniteSky.Domain.Identifiers;

namespace InfiniteSky.Domain.Items;

/// <summary>
/// An item assigned to a quick-use slot.
/// </summary>
public sealed record QuickUseItem(ItemDefinitionId ItemId, long Amount);
