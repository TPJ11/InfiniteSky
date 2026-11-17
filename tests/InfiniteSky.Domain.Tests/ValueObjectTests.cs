using InfiniteSky.Domain.Values;

namespace InfiniteSky.Domain.Tests;

public sealed class ValueObjectTests
{
    [Fact]
    public void PositionPreservesSignedAndFractionalJavascriptNumberValues()
    {
        var position = new Position3(-630.25, 50.5, 1575.75);

        Assert.Equal(-630.25, position.X);
        Assert.Equal(50.5, position.Y);
        Assert.Equal(1575.75, position.Z);
        Assert.Equal(new Position3(-630.25, 50.5, 1575.75), position);
    }

    [Fact]
    public void PositionDefaultMatchesCVec3ZeroDefault()
    {
        Assert.Equal(new Position3(0, 0, 0), Position3.Zero);
    }

    [Fact]
    public void DirectionDoesNotInventAnAngleConstraint()
    {
        var overFullTurn = new DirectionDegrees(361.5);
        var negative = new DirectionDegrees(-10);

        Assert.Equal(361.5, overFullTurn.Value);
        Assert.Equal(-10, negative.Value);
    }

    [Fact]
    public void RotationAndGridOrderingAreValueBased()
    {
        Assert.Equal(new Rotation2(1, 2), new Rotation2(1, 2));
        Assert.Equal(new GridPosition(7, 3), new GridPosition(7, 3));
        Assert.NotEqual(new GridPosition(3, 7), new GridPosition(7, 3));
    }
}
