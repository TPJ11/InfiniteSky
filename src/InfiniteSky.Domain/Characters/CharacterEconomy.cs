namespace InfiniteSky.Domain.Characters;

/// <summary>
/// Persistent character-owned and storage currency values.
/// </summary>
public sealed record CharacterEconomy
{
    public long Silver { get; init; }
    public long StorageSilver { get; init; }
    public long BankSilver { get; init; }
    public long StorageUse { get; init; }
    public long SilverBig { get; init; }
}
