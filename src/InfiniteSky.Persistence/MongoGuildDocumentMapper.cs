using InfiniteSky.Domain.Guilds;

using MongoDB.Bson;

namespace InfiniteSky.Persistence;

/// <summary>
/// Maps only fields consumed by the legacy world guild bootstrap.
/// </summary>
public static class MongoGuildDocumentMapper
{
    public static Guild Map(BsonDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!document.TryGetValue("Name", out BsonValue? nameValue)
            || nameValue.BsonType != BsonType.String)
        {
            throw new MongoPersistenceException(MongoPersistenceFailureKind.MalformedDocument);
        }

        var guild = new Guild(nameValue.AsString)
        {
            Level = ReadOptionalInteger(document, "Level", defaultValue: 0),
            HasGuildEmblem = ReadOptionalBoolean(document, "hasGuildEmblem", defaultValue: false),
        };

        if (!document.TryGetValue("Members", out BsonValue? membersValue))
        {
            return guild;
        }

        if (membersValue.BsonType != BsonType.Array)
        {
            throw new MongoPersistenceException(MongoPersistenceFailureKind.MalformedDocument);
        }

        foreach (BsonValue memberValue in membersValue.AsBsonArray)
        {
            if (memberValue.IsBsonNull)
            {
                guild.Members.Add(null);
                continue;
            }

            if (memberValue.BsonType != BsonType.Document
                || !memberValue.AsBsonDocument.TryGetValue("Name", out BsonValue? memberName)
                || memberName.BsonType != BsonType.String
                || !memberValue.AsBsonDocument.TryGetValue("LeaderFlag", out BsonValue? roleValue))
            {
                throw new MongoPersistenceException(MongoPersistenceFailureKind.MalformedDocument);
            }

            int role = ReadInteger(roleValue);
            if (role is < (int)GuildMemberRole.Member or > (int)GuildMemberRole.Master)
            {
                throw new MongoPersistenceException(MongoPersistenceFailureKind.MalformedDocument);
            }

            guild.Members.Add(new GuildMember(memberName.AsString, (GuildMemberRole)role));
        }

        return guild;
    }

    private static int ReadOptionalInteger(
        BsonDocument document,
        string fieldName,
        int defaultValue)
    {
        return document.TryGetValue(fieldName, out BsonValue? value)
            ? ReadInteger(value)
            : defaultValue;
    }

    private static int ReadInteger(BsonValue value)
    {
        if (!value.IsNumeric)
        {
            throw new MongoPersistenceException(MongoPersistenceFailureKind.MalformedDocument);
        }

        double numericValue = value.ToDouble();
        if (!double.IsFinite(numericValue)
            || Math.Truncate(numericValue) != numericValue
            || numericValue < int.MinValue
            || numericValue > int.MaxValue)
        {
            throw new MongoPersistenceException(MongoPersistenceFailureKind.MalformedDocument);
        }

        return checked((int)numericValue);
    }

    private static bool ReadOptionalBoolean(
        BsonDocument document,
        string fieldName,
        bool defaultValue)
    {
        if (!document.TryGetValue(fieldName, out BsonValue? value))
        {
            return defaultValue;
        }

        if (value.BsonType != BsonType.Boolean)
        {
            throw new MongoPersistenceException(MongoPersistenceFailureKind.MalformedDocument);
        }

        return value.AsBoolean;
    }
}
