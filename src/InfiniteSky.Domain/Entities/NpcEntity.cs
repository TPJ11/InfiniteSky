using InfiniteSky.Domain.Definitions;
using InfiniteSky.Domain.Identifiers;

namespace InfiniteSky.Domain.Entities;

/// <summary>
/// Runtime NPC identity and mutable state.
/// </summary>
public sealed class NpcEntity
{
    public NpcEntity(EntityId id, NpcDefinition definition)
    {
        Id = id;
        Definition = definition;
        State = new EntityState
        {
            UniqueId = id,
            OtherId = definition.Id.Value,
            Health = 1,
        };
    }

    public EntityId Id { get; }
    public NpcDefinition Definition { get; }
    public EntityState State { get; }
}
