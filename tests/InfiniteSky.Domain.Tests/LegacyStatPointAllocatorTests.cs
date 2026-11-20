using InfiniteSky.Domain.Characters;
using InfiniteSky.Domain.Progression;

namespace InfiniteSky.Domain.Tests;

public sealed class LegacyStatPointAllocatorTests
{
    [Theory]
    [InlineData(0, 1, "Strength")]
    [InlineData(1, 1, "Chi")]
    [InlineData(2, 1, "Dexterity")]
    [InlineData(3, 1, "Vitality")]
    [InlineData(4, 5, "Strength")]
    [InlineData(5, 5, "Chi")]
    [InlineData(6, 5, "Dexterity")]
    [InlineData(7, 5, "Vitality")]
    public void MapsLegacyKeysAndOverridesRequestedAmount(uint key, int expectedAmount, string expectedAttribute)
    {
        var attributes = new CharacterAttributes();
        var progression = new CharacterProgression { StatPoints = 10 };

        LegacyStatPointAllocation result = LegacyStatPointAllocator.Apply(
            attributes,
            progression,
            key,
            99);

        Assert.Equal(LegacyStatPointAllocationStatus.Applied, result.Status);
        Assert.Equal(10 - expectedAmount, result.Progression.StatPoints);
        Assert.Equal(expectedAmount, expectedAttribute switch
        {
            "Strength" => result.Attributes.Strength,
            "Chi" => result.Attributes.Chi,
            "Dexterity" => result.Attributes.Dexterity,
            "Vitality" => result.Attributes.Vitality,
            _ => throw new InvalidOperationException("Unexpected test attribute."),
        });
    }

    [Fact]
    public void RejectsFivePointAllocationWhenInsufficientPointsRemain()
    {
        var attributes = new CharacterAttributes { Vitality = 2 };
        var progression = new CharacterProgression { StatPoints = 4 };

        LegacyStatPointAllocation result = LegacyStatPointAllocator.Apply(attributes, progression, 7, 1);

        Assert.Equal(LegacyStatPointAllocationStatus.InsufficientPoints, result.Status);
        Assert.Same(attributes, result.Attributes);
        Assert.Same(progression, result.Progression);
    }

    [Fact]
    public void ReturnsWithoutMutationWhenNoPointsAreAvailable()
    {
        var attributes = new CharacterAttributes { Strength = 3 };
        var progression = new CharacterProgression { StatPoints = 0 };

        LegacyStatPointAllocation result = LegacyStatPointAllocator.Apply(attributes, progression, 0, 1);

        Assert.Equal(LegacyStatPointAllocationStatus.NoPointsAvailable, result.Status);
        Assert.Same(attributes, result.Attributes);
        Assert.Same(progression, result.Progression);
    }

    [Theory]
    [InlineData(1, LegacyStatPointAllocationStatus.InvalidKey)]
    [InlineData(6, LegacyStatPointAllocationStatus.InsufficientPoints)]
    public void PreservesInvalidKeyHandlingAgainstTheProvidedValue(
        uint requestedValue,
        LegacyStatPointAllocationStatus expectedStatus)
    {
        var attributes = new CharacterAttributes();
        var progression = new CharacterProgression { StatPoints = 5 };

        LegacyStatPointAllocation result = LegacyStatPointAllocator.Apply(
            attributes,
            progression,
            8,
            requestedValue);

        Assert.Equal(expectedStatus, result.Status);
    }
}
