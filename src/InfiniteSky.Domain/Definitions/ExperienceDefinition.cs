namespace InfiniteSky.Domain.Definitions;

/// <summary>
/// One level record from the original experience information file.
/// </summary>
public sealed record ExperienceDefinition
{
    public uint Level { get; init; }
    public uint ExperienceStart { get; init; }
    public uint ExperienceEnd { get; init; }
    public uint SkillPoint { get; init; }
    public uint GuanyinDamage { get; init; }
    public uint FujinDamage { get; init; }
    public uint JinongDamage { get; init; }
    public uint GuanyinDefense { get; init; }
    public uint FujinDefense { get; init; }
    public uint JinongDefense { get; init; }
    public uint GuanyinHitRate { get; init; }
    public uint FujinHitRate { get; init; }
    public uint JinongHitRate { get; init; }
    public uint GuanyinDodge { get; init; }
    public uint FujinDodge { get; init; }
    public uint JinongDodge { get; init; }
    public uint LightDamage { get; init; }
    public uint ShadowDamage { get; init; }
    public uint DarkDamage { get; init; }
    public uint GuanyinHealth { get; init; }
    public uint FujinHealth { get; init; }
    public uint JinongHealth { get; init; }
    public uint GuanyinChi { get; init; }
    public uint FujinChi { get; init; }
    public uint JinongChi { get; init; }
}
