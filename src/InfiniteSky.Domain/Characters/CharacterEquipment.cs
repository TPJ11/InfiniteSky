using InfiniteSky.Domain.Items;

namespace InfiniteSky.Domain.Characters;

/// <summary>
/// Persistent equipment slots. Null is the source default for an empty slot.
/// </summary>
public sealed class CharacterEquipment
{
    public EquipmentItem? Ring { get; set; }
    public EquipmentItem? Cape { get; set; }
    public EquipmentItem? Armor { get; set; }
    public EquipmentItem? Glove { get; set; }
    public EquipmentItem? Amulet { get; set; }
    public EquipmentItem? Boot { get; set; }
    public EquipmentItem? CalabashBottle { get; set; }
    public EquipmentItem? Weapon { get; set; }
    public PetEquipment? Pet { get; set; }
}
