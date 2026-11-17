using InfiniteSky.Domain.Definitions;
using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Values;

namespace InfiniteSky.Domain.Entities;

/// <summary>
/// Runtime item entity placed in a zone.
/// </summary>
public sealed class WorldItemEntity
{
    public WorldItemEntity(
        EntityId id,
        ItemDefinition definition)
    {
        Id = id;
        Definition = definition;
        State = new EntityState
        {
            UniqueId = id,
            Health = 1,
        };
    }

    public EntityId Id { get; }
    public ItemDefinition Definition { get; }
    public EntityState State { get; }
    public long Amount { get; set; } = 1;
    public int Enchant { get; set; }
    public int Combine { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public Rotation2 Rotation { get; set; }
    public int JustSpawned { get; set; } = 1;
}
