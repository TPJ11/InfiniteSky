using InfiniteSky.Domain.Identifiers;

namespace InfiniteSky.Domain.Skills;

/// <summary>
/// A learned, configured, or bar-assigned skill entry.
/// </summary>
public sealed record SkillEntry(SkillDefinitionId SkillId, int Level);
