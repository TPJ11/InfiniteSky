using InfiniteSky.Domain.Characters;

namespace InfiniteSky.Domain.Progression;

public enum LegacyStatPointAllocationStatus
{
    Applied,
    NoPointsAvailable,
    InsufficientPoints,
    InvalidKey,
}

public readonly record struct LegacyStatPointAllocation(
    CharacterAttributes Attributes,
    CharacterProgression Progression,
    LegacyStatPointAllocationStatus Status);

public static class LegacyStatPointAllocator
{
    public static LegacyStatPointAllocation Apply(
        CharacterAttributes attributes,
        CharacterProgression progression,
        uint key,
        uint requestedValue)
    {
        ArgumentNullException.ThrowIfNull(attributes);
        ArgumentNullException.ThrowIfNull(progression);

        if (progression.StatPoints == 0)
        {
            return Result(attributes, progression, LegacyStatPointAllocationStatus.NoPointsAvailable);
        }

        (int points, StatAttribute stat) = key switch
        {
            0 => (1, StatAttribute.Strength),
            1 => (1, StatAttribute.Chi),
            2 => (1, StatAttribute.Dexterity),
            3 => (1, StatAttribute.Vitality),
            4 => (5, StatAttribute.Strength),
            5 => (5, StatAttribute.Chi),
            6 => (5, StatAttribute.Dexterity),
            7 => (5, StatAttribute.Vitality),
            _ => (0, StatAttribute.None),
        };

        if (points == 0)
        {
            return progression.StatPoints < requestedValue
                ? Result(attributes, progression, LegacyStatPointAllocationStatus.InsufficientPoints)
                : Result(attributes, progression, LegacyStatPointAllocationStatus.InvalidKey);
        }

        if (progression.StatPoints < points)
        {
            return Result(attributes, progression, LegacyStatPointAllocationStatus.InsufficientPoints);
        }

        CharacterAttributes updatedAttributes = stat switch
        {
            StatAttribute.Strength => attributes with { Strength = checked(attributes.Strength + points) },
            StatAttribute.Chi => attributes with { Chi = checked(attributes.Chi + points) },
            StatAttribute.Dexterity => attributes with { Dexterity = checked(attributes.Dexterity + points) },
            StatAttribute.Vitality => attributes with { Vitality = checked(attributes.Vitality + points) },
            _ => throw new InvalidOperationException("The legacy stat key did not resolve to an attribute."),
        };
        CharacterProgression updatedProgression = progression with
        {
            StatPoints = progression.StatPoints - points,
        };

        return Result(updatedAttributes, updatedProgression, LegacyStatPointAllocationStatus.Applied);
    }

    private static LegacyStatPointAllocation Result(
        CharacterAttributes attributes,
        CharacterProgression progression,
        LegacyStatPointAllocationStatus status)
        => new(attributes, progression, status);

    private enum StatAttribute
    {
        None,
        Strength,
        Chi,
        Dexterity,
        Vitality,
    }
}
