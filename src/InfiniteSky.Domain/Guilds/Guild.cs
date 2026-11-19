namespace InfiniteSky.Domain.Guilds;

/// <summary>
/// Guild aggregate backed by a case-sensitive name and ordered member array.
/// </summary>
public sealed class Guild
{
    public Guild(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
    }

    public string Name { get; }
    public int Level { get; set; }
    public bool HasGuildEmblem { get; set; }
    public IList<GuildMember?> Members { get; } = [];
}
