using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Values;

namespace InfiniteSky.Domain.Entities;

/// <summary>
/// Shared mutable state emitted for monster, NPC, and item-like world objects.
/// </summary>
public sealed class EntityState
{
    public EntityId? UniqueId { get; set; }
    public uint LocalId { get; set; }
    public uint OtherId { get; set; }
    public int Life { get; set; } = 1;
    public uint Stance { get; set; }
    public uint Skill { get; set; }
    public double Frame { get; set; }
    public Position3 Location { get; set; } = Position3.Zero;
    public Position3 LocationTo { get; set; } = Position3.Zero;
    public DirectionDegrees Direction { get; set; } = DirectionDegrees.Zero;
    public DirectionDegrees TargetDirection { get; set; } = DirectionDegrees.Zero;
    public DirectionDegrees FacingDirection { get; set; } = DirectionDegrees.Zero;
    public int? TargetObjectIndex { get; set; }
    public double Health { get; set; }
}
