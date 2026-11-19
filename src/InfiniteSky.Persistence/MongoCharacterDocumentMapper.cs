using InfiniteSky.Domain.Characters;
using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Items;
using InfiniteSky.Domain.Skills;
using InfiniteSky.Domain.Values;

using MongoDB.Bson;

namespace InfiniteSky.Persistence;

/// <summary>
/// Maps only fields represented by the current character domain model.
/// </summary>
public static class MongoCharacterDocumentMapper
{
    public static Character Map(BsonDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        long characterId = ReadRequiredLong(document, "_id");
        long accountId = ReadRequiredLong(document, "AccountID");
        if (characterId < 0 || accountId < 0)
        {
            throw MalformedDocument();
        }

        string name = ReadRequiredString(document, "Name");
        int? savedZoneId = ReadNullableInt(document, "MapID");
        if (savedZoneId < 0)
        {
            throw MalformedDocument();
        }

        var character = new Character(new CharacterId(characterId), new AccountId(accountId), name)
        {
            ServerName = ReadNullableString(document, "ServerName"),
            GuildName = ReadNullableString(document, "GuildName"),
            Title = ReadNullableString(document, "Title"),
            IsGameMaster = ReadInt(document, "IsGM", 0),
            Appearance = ReadAppearance(document),
            Progression = new CharacterProgression
            {
                Level = ReadInt(document, "Level", 1),
                Experience = ReadLong(document, "Experience", 0),
                OtherIngame = ReadInt(document, "OtherIngame", 0),
                StatBonus = ReadInt(document, "StatBonus", 0),
                Honor = ReadInt(document, "Honor", 0),
                SkillPoints = ReadInt(document, "SkillPoints", 0),
                StatPoints = ReadInt(document, "StatPoints", 0),
                PlayTime = ReadLong(document, "PlayTime", 0),
                ContributionPoints = ReadInt(document, "CP", 0),
            },
            Attributes = new CharacterAttributes
            {
                Strength = ReadInt(document, "StatStrength", 0),
                Chi = ReadInt(document, "StatChi", 0),
                Dexterity = ReadInt(document, "StatDexterity", 0),
                Vitality = ReadInt(document, "StatVitality", 0),
            },
            Economy = new CharacterEconomy
            {
                StorageUse = ReadLong(document, "StorageUse", 0),
                Silver = ReadLong(document, "Silver", 0),
                StorageSilver = ReadLong(document, "StorageSilver", 0),
                BankSilver = ReadLong(document, "BankSilver", 0),
                SilverBig = ReadLong(document, "SilverBig", 0),
            },
            Vitals = new CharacterVitals
            {
                Health = ReadNullableDouble(document, "Health"),
                Chi = ReadNullableDouble(document, "Chi"),
            },
            Bonuses = new CharacterBonuses
            {
                StrengthBonus = ReadInt(document, "StrBonus", 0),
                DexterityBonus = ReadInt(document, "DexBonus", 0),
                LuckBuff = ReadInt(document, "LuckBuff", 0),
                StrengthBuff = ReadInt(document, "StrengthBuff", 0),
                ExperienceBuff = ReadInt(document, "ExperienceBuff", 0),
                AutoPillHealth = ReadInt(document, "AutoPillHP", 0),
                AutoPillChi = ReadInt(document, "AutoPillChi", 0),
                ElementalDamage = ReadInt(document, "ElementalDamage", 0),
                ElementalDefense = ReadInt(document, "ElementalDefense", 0),
                DarkDamage = ReadInt(document, "DarkDamage", 0),
                FactionDefenseBonus = ReadInt(document, "FactionDefenseBonus", 0),
                ChanceDodgeHit = ReadInt(document, "ChanceDodge_Hit", 0),
                DamageBonus = ReadInt(document, "DamageBonus", 0),
                DailyOne = ReadInt(document, "Daily1", 0),
                DailyPlayerVersusPlayerKills = ReadInt(document, "DailyPvPKill", 0),
                DailyUnknown = ReadInt(document, "DailyUnknown", 0),
                DailyUnknown2 = ReadInt(document, "DailyUnknown2", 0),
            },
            SavedLocation = new SavedCharacterLocation
            {
                ZoneId = savedZoneId is int zoneId
                    ? new ZoneId(zoneId)
                    : null,
                X = ReadNullableDouble(document, "RealX"),
                Y = ReadNullableDouble(document, "RealY"),
                Z = ReadNullableDouble(document, "RealZ"),
            },
            Deaths = ReadLong(document, "Deaths", 0),
            LastUpdated = ReadNullableDateTime(document, "LastUpdated"),
        };

        MapEquipment(document, character.Equipment);
        MapArray(document, "Inventory", character.Inventory, ReadInventoryItem);
        MapArray(document, "Storage", character.Storage, ReadInventoryItem);
        MapArray(document, "QuickUseItems", character.QuickUseItems, ReadQuickUseItem);
        MapArray(document, "SkillList", character.SkillList, ReadSkillEntry);
        MapArray(document, "Skills", character.Skills, ReadSkillEntry);
        MapArray(document, "SkillBar", character.SkillBar, ReadSkillEntry);
        MapFriends(document, character.Friends);

        return character;
    }

    private static CharacterAppearance ReadAppearance(BsonDocument document)
    {
        return new CharacterAppearance
        {
            Clan = (Clan)ReadByte(document, "Clan", 0),
            Gender = (Gender)ReadByte(document, "Gender", 0),
            Hair = ReadInt(document, "Hair", 0),
            Face = ReadInt(document, "Face", 0),
        };
    }

    private static void MapEquipment(BsonDocument document, CharacterEquipment equipment)
    {
        equipment.Ring = ReadEquipment(document, "Ring");
        equipment.Cape = ReadEquipment(document, "Cape");
        equipment.Armor = ReadEquipment(document, "Armor");
        equipment.Glove = ReadEquipment(document, "Glove");
        equipment.Amulet = ReadEquipment(document, "Amulet");
        equipment.Boot = ReadEquipment(document, "Boot");
        equipment.CalabashBottle = ReadEquipment(document, "CalbashBottle");
        equipment.Weapon = ReadEquipment(document, "Weapon");

        BsonValue? petValue = Get(document, "Pet");
        if (petValue is not null && !petValue.IsBsonNull)
        {
            BsonDocument pet = RequireDocument(petValue);
            equipment.Pet = new PetEquipment
            {
                ItemId = new ItemDefinitionId(ReadUInt(pet, "ID", 0)),
                Activity = ReadLong(pet, "Activity", 0),
                Growth = ReadLong(pet, "Growth", 0),
            };
        }
    }

    private static EquipmentItem? ReadEquipment(BsonDocument document, string field)
    {
        BsonValue? value = Get(document, field);
        if (value is null || value.IsBsonNull)
        {
            return null;
        }

        BsonDocument item = RequireDocument(value);
        return new EquipmentItem
        {
            ItemId = new ItemDefinitionId(ReadUInt(item, "ID", 0)),
            Enchant = ReadInt(item, "Enchant", 0),
            Combine = ReadInt(item, "Combine", 0),
        };
    }

    private static InventoryItem ReadInventoryItem(BsonDocument document)
    {
        uint itemId = ReadRequiredUInt(document, "ID");
        bool hasColumn = HasNonNull(document, "Column");
        bool hasRow = HasNonNull(document, "Row");
        if (hasColumn != hasRow)
        {
            throw MalformedDocument();
        }

        GridPosition? position = hasColumn
            ? new GridPosition(
                ReadUInt(document, "Column", 0),
                ReadUInt(document, "Row", 0))
            : null;

        return new InventoryItem
        {
            ItemId = new ItemDefinitionId(itemId),
            Position = position,
            Amount = ReadNullableLong(document, "Amount"),
            Enchant = ReadNullableInt(document, "Enchant"),
            Combine = ReadNullableInt(document, "Combine"),
            Activity = ReadNullableLong(document, "Activity"),
            Growth = ReadNullableLong(document, "Growth"),
        };
    }

    private static QuickUseItem ReadQuickUseItem(BsonDocument document)
    {
        long amount = HasNonNull(document, "Ammount")
            ? ReadLong(document, "Ammount", 0)
            : ReadLong(document, "Amount", 0);
        return new QuickUseItem(
            new ItemDefinitionId(ReadRequiredUInt(document, "ID")),
            amount);
    }

    private static SkillEntry ReadSkillEntry(BsonDocument document)
    {
        return new SkillEntry(
            new SkillDefinitionId(ReadRequiredUInt(document, "ID")),
            ReadInt(document, "Level", 0));
    }

    private static void MapArray<T>(
        BsonDocument document,
        string field,
        IList<T?> target,
        Func<BsonDocument, T> map)
        where T : class
    {
        BsonValue? value = Get(document, field);
        if (value is null || value.IsBsonNull)
        {
            return;
        }

        if (value.BsonType != BsonType.Array)
        {
            throw MalformedDocument();
        }

        foreach (BsonValue item in value.AsBsonArray)
        {
            target.Add(item.IsBsonNull ? null : map(RequireDocument(item)));
        }
    }

    private static void MapFriends(BsonDocument document, IList<string> target)
    {
        BsonValue? value = Get(document, "Friends");
        if (value is null || value.IsBsonNull)
        {
            return;
        }

        if (value.BsonType != BsonType.Array)
        {
            throw MalformedDocument();
        }

        foreach (BsonValue friend in value.AsBsonArray)
        {
            if (friend.BsonType != BsonType.String)
            {
                throw MalformedDocument();
            }

            target.Add(friend.AsString);
        }
    }

    private static BsonDocument RequireDocument(BsonValue value)
    {
        if (value.BsonType != BsonType.Document)
        {
            throw MalformedDocument();
        }

        return value.AsBsonDocument;
    }

    private static string ReadRequiredString(BsonDocument document, string field)
    {
        BsonValue? value = Get(document, field);
        return value?.BsonType == BsonType.String
            ? value.AsString
            : throw MalformedDocument();
    }

    private static string? ReadNullableString(BsonDocument document, string field)
    {
        BsonValue? value = Get(document, field);
        if (value is null || value.IsBsonNull)
        {
            return null;
        }

        return value.BsonType == BsonType.String
            ? value.AsString
            : throw MalformedDocument();
    }

    private static long ReadRequiredLong(BsonDocument document, string field)
    {
        BsonValue? value = Get(document, field);
        return value is null || value.IsBsonNull
            ? throw MalformedDocument()
            : ReadLong(value);
    }

    private static long ReadLong(BsonDocument document, string field, long defaultValue)
    {
        BsonValue? value = Get(document, field);
        return value is null || value.IsBsonNull ? defaultValue : ReadLong(value);
    }

    private static long ReadLong(BsonValue value)
    {
        return value.BsonType switch
        {
            BsonType.Int32 => value.AsInt32,
            BsonType.Int64 => value.AsInt64,
            BsonType.Double => ReadIntegralDouble(value.AsDouble),
            _ => throw MalformedDocument(),
        };
    }

    private static long ReadIntegralDouble(double value)
    {
        const double exclusiveUpperBound = 9_223_372_036_854_775_808d;
        if (!double.IsFinite(value)
            || Math.Truncate(value) != value
            || value < long.MinValue
            || value >= exclusiveUpperBound)
        {
            throw MalformedDocument();
        }

        return checked((long)value);
    }

    private static int ReadInt(BsonDocument document, string field, int defaultValue)
    {
        long value = ReadLong(document, field, defaultValue);
        return value is >= int.MinValue and <= int.MaxValue
            ? (int)value
            : throw MalformedDocument();
    }

    private static byte ReadByte(BsonDocument document, string field, byte defaultValue)
    {
        long value = ReadLong(document, field, defaultValue);
        return value is >= byte.MinValue and <= byte.MaxValue
            ? (byte)value
            : throw MalformedDocument();
    }

    private static uint ReadRequiredUInt(BsonDocument document, string field)
    {
        BsonValue? value = Get(document, field);
        if (value is null || value.IsBsonNull)
        {
            throw MalformedDocument();
        }

        return ReadUInt(value);
    }

    private static uint ReadUInt(BsonDocument document, string field, uint defaultValue)
    {
        BsonValue? value = Get(document, field);
        return value is null || value.IsBsonNull ? defaultValue : ReadUInt(value);
    }

    private static uint ReadUInt(BsonValue value)
    {
        long numeric = ReadLong(value);
        return numeric is >= uint.MinValue and <= uint.MaxValue
            ? (uint)numeric
            : throw MalformedDocument();
    }

    private static int? ReadNullableInt(BsonDocument document, string field)
    {
        BsonValue? value = Get(document, field);
        if (value is null || value.IsBsonNull)
        {
            return null;
        }

        long numeric = ReadLong(value);
        return numeric is >= int.MinValue and <= int.MaxValue
            ? (int)numeric
            : throw MalformedDocument();
    }

    private static long? ReadNullableLong(BsonDocument document, string field)
    {
        BsonValue? value = Get(document, field);
        return value is null || value.IsBsonNull ? null : ReadLong(value);
    }

    private static double? ReadNullableDouble(BsonDocument document, string field)
    {
        BsonValue? value = Get(document, field);
        if (value is null || value.IsBsonNull)
        {
            return null;
        }

        double result = value.BsonType switch
        {
            BsonType.Int32 => value.AsInt32,
            BsonType.Int64 => value.AsInt64,
            BsonType.Double => value.AsDouble,
            _ => throw MalformedDocument(),
        };
        return double.IsFinite(result) ? result : throw MalformedDocument();
    }

    private static DateTimeOffset? ReadNullableDateTime(BsonDocument document, string field)
    {
        BsonValue? value = Get(document, field);
        if (value is null || value.IsBsonNull)
        {
            return null;
        }

        return value.BsonType == BsonType.DateTime
            ? new DateTimeOffset(value.AsBsonDateTime.ToUniversalTime())
            : throw MalformedDocument();
    }

    private static bool HasNonNull(BsonDocument document, string field)
    {
        BsonValue? value = Get(document, field);
        return value is not null && !value.IsBsonNull;
    }

    private static BsonValue? Get(BsonDocument document, string field)
        => document.TryGetValue(field, out BsonValue? value) ? value : null;

    private static MongoPersistenceException MalformedDocument()
        => new(MongoPersistenceFailureKind.MalformedDocument);
}
