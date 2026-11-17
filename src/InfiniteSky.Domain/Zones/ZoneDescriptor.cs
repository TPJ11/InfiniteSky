using InfiniteSky.Domain.Identifiers;

namespace InfiniteSky.Domain.Zones;

/// <summary>
/// Configured zone identity and operator display metadata.
/// </summary>
public sealed record ZoneDescriptor
{
    public required ZoneId Id { get; init; }
    public required string Name { get; init; }
    public string? DisplayName { get; init; }
    public bool Load { get; init; }
}
