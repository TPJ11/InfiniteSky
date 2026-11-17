namespace InfiniteSky.Domain.Guilds;

/// <summary>
/// Ordered guild member entry with the original role value.
/// </summary>
public sealed record GuildMember(string Name, GuildMemberRole Role);
