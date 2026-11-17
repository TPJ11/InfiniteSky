using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Items;
using InfiniteSky.Domain.Skills;

namespace InfiniteSky.Domain.Characters;

/// <summary>
/// Persistent character aggregate. Runtime movement, timers, packet state, and sockets are excluded.
/// </summary>
public sealed class Character
{
    /// <summary>
    /// Initializes a character with the source-required identity fields.
    /// </summary>
    public Character(CharacterId id, AccountId accountId, string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Id = id;
        AccountId = accountId;
        Name = name;
    }

    public CharacterId Id { get; }
    public AccountId AccountId { get; }
    public string Name { get; }
    public string? ServerName { get; init; }
    public string? GuildName { get; set; }
    public string? Title { get; set; }
    public int IsGameMaster { get; set; }
    public CharacterAppearance Appearance { get; init; } = new();
    public CharacterProgression Progression { get; set; } = new();
    public CharacterAttributes Attributes { get; set; } = new();
    public CharacterEconomy Economy { get; set; } = new();
    public CharacterVitals Vitals { get; set; } = new();
    public CharacterBonuses Bonuses { get; set; } = new();
    public SavedCharacterLocation SavedLocation { get; set; } = new();
    public CharacterEquipment Equipment { get; } = new();

    /// <summary>
    /// Keeps original array order and represents undefined/null slots as null.
    /// </summary>
    public IList<InventoryItem?> Inventory { get; } = [];

    public IList<QuickUseItem?> QuickUseItems { get; } = [];
    public IList<SkillEntry?> SkillList { get; } = [];
    public IList<SkillEntry?> Skills { get; } = [];
    public IList<SkillEntry?> SkillBar { get; } = [];
    public IList<InventoryItem?> Storage { get; } = [];
    public IList<string> Friends { get; } = [];

    public long Deaths { get; set; }
    public DateTimeOffset? LastUpdated { get; set; }
}
