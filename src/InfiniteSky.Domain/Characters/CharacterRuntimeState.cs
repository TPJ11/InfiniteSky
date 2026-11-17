using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Values;

namespace InfiniteSky.Domain.Characters;

/// <summary>
/// Ephemeral character state attached to a connected world actor.
/// </summary>
public sealed class CharacterRuntimeState
{
    public AccountId? AccountId { get; set; }
    public CharacterId? CharacterId { get; set; }
    public EntityId? UniqueId { get; set; }
    public string Name { get; set; } = string.Empty;
    public CharacterAppearance Appearance { get; set; } = new();
    public CharacterEquipment Equipment { get; } = new();
    public int Level { get; set; }
    public double Honor { get; set; }
    public string GuildName { get; set; } = string.Empty;
    public string GuildTag { get; set; } = string.Empty;
    public bool GuildTagExists { get; set; }
    public uint Stance { get; set; }
    public uint Skill { get; set; }
    public double Frame { get; set; }
    public Position3 Location { get; set; } = Position3.Zero;
    public Position3 LocationTo { get; set; } = Position3.Zero;
    public Position3 ProposedLocation { get; set; } = Position3.Zero;
    public DirectionDegrees Direction { get; set; } = DirectionDegrees.Zero;
    public DirectionDegrees FacingDirection { get; set; } = DirectionDegrees.Zero;
    public EntityId? TargetObjectIndex { get; set; }
    public EntityId? TargetObjectUniqueNumber { get; set; }
    public double CurrentHealth { get; set; }
    public double MaximumHealth { get; set; }
    public double CurrentChi { get; set; }
    public double MaximumChi { get; set; }
    public bool Hidden { get; set; }
}
