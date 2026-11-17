using InfiniteSky.Domain.Definitions;
using InfiniteSky.Domain.Identifiers;

namespace InfiniteSky.Domain.Entities;

/// <summary>
/// Runtime monster identity and mutable state, separate from its static definition.
/// </summary>
public sealed class MonsterEntity
{
    public MonsterEntity(
        EntityId id,
        MonsterDefinition definition)
    {
        Id = id;
        Definition = definition;
        State = new EntityState
        {
            UniqueId = id,
            OtherId = definition.Id.Value,
            Health = definition.Health,
        };
    }

    public EntityId Id { get; }
    public MonsterDefinition Definition { get; }
    public EntityState State { get; }
    public double MaximumHealth => Definition.Health;
}
