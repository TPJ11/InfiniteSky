using InfiniteSky.Domain.Guilds;

namespace InfiniteSky.Application.Guilds;

/// <summary>
/// Loads the legacy guild snapshot required during world bootstrap.
/// </summary>
public interface IGuildRepository
{
    ValueTask<IReadOnlyList<Guild>> LoadAllAsync(CancellationToken cancellationToken);
}
