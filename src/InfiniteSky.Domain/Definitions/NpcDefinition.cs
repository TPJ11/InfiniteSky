using InfiniteSky.Domain.Identifiers;

namespace InfiniteSky.Domain.Definitions;

/// <summary>
/// Named NPC data with the five dialogue strings visible in the original reader.
/// </summary>
public sealed record NpcDefinition
{
    public required NpcDefinitionId Id { get; init; }
    public required string Name { get; init; }
    public uint PageCount { get; init; }
    public string Chat1 { get; init; } = string.Empty;
    public string Chat2 { get; init; } = string.Empty;
    public string Chat3 { get; init; } = string.Empty;
    public string Chat4 { get; init; } = string.Empty;
    public string Chat5 { get; init; } = string.Empty;
}
