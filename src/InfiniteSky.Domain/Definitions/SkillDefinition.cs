using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Skills;

namespace InfiniteSky.Domain.Definitions;

/// <summary>
/// Named static skill data and start/end modifier records.
/// </summary>
public sealed record SkillDefinition
{
    public required SkillDefinitionId Id { get; init; }
    public required string Name { get; init; }
    public SkillCategory Category { get; init; }
    public uint SpriteStartId { get; init; }
    public uint Clan { get; init; }
    public uint Weapon { get; init; }
    public string Description1 { get; init; } = string.Empty;
    public string Description2 { get; init; } = string.Empty;
    public string Description3 { get; init; } = string.Empty;
    public int ChiUsage { get; init; }
    public int PointsToLearn { get; init; }
    public int MaximumLevel { get; init; }
    public SkillModifiers ModifiersStart { get; init; } = new();
    public SkillModifiers ModifiersEnd { get; init; } = new();
}
