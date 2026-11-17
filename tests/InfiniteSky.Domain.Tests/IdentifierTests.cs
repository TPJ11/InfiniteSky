using InfiniteSky.Domain.Identifiers;

namespace InfiniteSky.Domain.Tests;

public sealed class IdentifierTests
{
    [Fact]
    public void IdentifiersUseValueEqualityWithinTheirOwnType()
    {
        var first = new CharacterId(42);
        var second = new CharacterId(42);
        var different = new CharacterId(43);

        Assert.Equal(first, second);
        Assert.NotEqual(first, different);
        Assert.False(typeof(CharacterId).IsAssignableFrom(typeof(AccountId)));
        Assert.Equal("42", first.ToString());
    }

    [Fact]
    public void CounterIdentifiersRejectNegativeValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AccountId(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CharacterId(-1));
    }

    [Fact]
    public void ZoneZeroIsPreservedForReturnToTownRequests()
    {
        var zero = new ZoneId(0);
        var normal = new ZoneId(119);

        Assert.Equal(0, zero.Value);
        Assert.Equal(119, normal.Value);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ZoneId(-1));
    }

    [Fact]
    public void RuntimeAndDefinitionIdentifiersPreserveUnsignedValues()
    {
        var entityId = new EntityId(uint.MaxValue);
        var itemId = new ItemDefinitionId(99_283);

        Assert.Equal(uint.MaxValue, entityId.Value);
        Assert.Equal((uint)99_283, itemId.Value);
    }
}
