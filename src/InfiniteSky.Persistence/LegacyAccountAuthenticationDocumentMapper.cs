using System.Text;

using InfiniteSky.Application.Authentication;
using InfiniteSky.Domain.Identifiers;

using MongoDB.Bson;

namespace InfiniteSky.Persistence;

/// <summary>
/// Reads only the legacy fields required by authentication without class-map conventions.
/// </summary>
public static class LegacyAccountAuthenticationDocumentMapper
{
    public static AccountAuthenticationRecord Map(BsonDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!document.TryGetValue("_id", out BsonValue? idValue)
            || !document.TryGetValue("Username", out BsonValue? usernameValue)
            || usernameValue.BsonType != BsonType.String)
        {
            throw new MongoPersistenceException(MongoPersistenceFailureKind.MalformedDocument);
        }

        long id = ReadNumericId(idValue);
        if (id < 0)
        {
            throw new MongoPersistenceException(MongoPersistenceFailureKind.MalformedDocument);
        }

        byte[]? password = null;
        if (document.TryGetValue("Password", out BsonValue? passwordValue)
            && !passwordValue.IsBsonNull)
        {
            if (passwordValue.BsonType != BsonType.String)
            {
                throw new MongoPersistenceException(MongoPersistenceFailureKind.MalformedDocument);
            }

            password = Encoding.UTF8.GetBytes(passwordValue.AsString);
        }

        return new AccountAuthenticationRecord(
            new AccountId(id),
            usernameValue.AsString,
            password);
    }

    private static long ReadNumericId(BsonValue value)
    {
        return value.BsonType switch
        {
            BsonType.Int32 => value.AsInt32,
            BsonType.Int64 => value.AsInt64,
            BsonType.Double => ReadIntegralDouble(value.AsDouble),
            _ => throw new MongoPersistenceException(MongoPersistenceFailureKind.MalformedDocument),
        };
    }

    private static long ReadIntegralDouble(double value)
    {
        const double exclusiveUpperBound = 9_223_372_036_854_775_808d;
        if (!double.IsFinite(value)
            || Math.Truncate(value) != value
            || value < 0
            || value >= exclusiveUpperBound)
        {
            throw new MongoPersistenceException(MongoPersistenceFailureKind.MalformedDocument);
        }

        return checked((long)value);
    }
}
