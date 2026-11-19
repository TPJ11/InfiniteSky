using InfiniteSky.Domain.Guilds;

namespace InfiniteSky.World;

public sealed record WorldGuildSnapshot
{
    public WorldGuildSnapshot(Guild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);
        Name = guild.Name;
        Level = guild.Level;
        HasGuildEmblem = guild.HasGuildEmblem;
        Members = Array.AsReadOnly(guild.Members.ToArray());
    }

    public string Name { get; }
    public int Level { get; }
    public bool HasGuildEmblem { get; }
    public IReadOnlyList<GuildMember?> Members { get; }
}
