using InfiniteSky.Domain.Characters;
using InfiniteSky.Domain.Definitions;
using InfiniteSky.Domain.Progression;

namespace InfiniteSky.Domain.Tests;

public sealed class LegacyExperienceAwarderTests
{
    [Fact]
    public void AddsExperienceWithoutChangingLevelBelowTheCurrentThreshold()
    {
        var progression = new CharacterProgression { Level = 1, Experience = 20 };
        IReadOnlyDictionary<uint, ExperienceDefinition> experience = Records(
            Record(1, 0, 100),
            Record(145, 10_000, 20_000));

        LegacyExperienceAward result = LegacyExperienceAwarder.Apply(progression, 10, experience);

        Assert.True(result.WasApplied);
        Assert.Equal(0, result.LevelsGained);
        Assert.Equal(1, result.Progression.Level);
        Assert.Equal(30, result.Progression.Experience);
    }

    [Fact]
    public void CrossesThresholdAndUsesNextLevelPointsAndLegacyExtraExperience()
    {
        var progression = new CharacterProgression { Level = 1, Experience = 90 };
        IReadOnlyDictionary<uint, ExperienceDefinition> experience = Records(
            Record(1, 0, 100),
            Record(2, 100, 200, skillPoints: 2),
            Record(145, 10_000, 20_000));

        LegacyExperienceAward result = LegacyExperienceAwarder.Apply(progression, 15, experience);

        Assert.True(result.WasApplied);
        Assert.Equal(1, result.LevelsGained);
        Assert.Equal(2, result.Progression.Level);
        Assert.Equal(106, result.Progression.Experience);
        Assert.Equal(2, result.Progression.SkillPoints);
        Assert.Equal(5, result.Progression.StatPoints);
    }

    [Theory]
    [InlineData(99, 100, 0)]
    [InlineData(112, 113, 30)]
    public void AppliesLegacyStatPointBandsAtTheNewLevel(int level, int nextLevel, int expectedStatPoints)
    {
        var progression = new CharacterProgression { Level = level, Experience = 10 };
        IReadOnlyDictionary<uint, ExperienceDefinition> experience = Records(
            Record((uint)level, 0, 10),
            Record((uint)nextLevel, 10, 100, skillPoints: 1),
            Record(145, 10_000, 20_000));

        LegacyExperienceAward result = LegacyExperienceAwarder.Apply(progression, 1, experience);

        Assert.Equal(1, result.LevelsGained);
        Assert.Equal(nextLevel, result.Progression.Level);
        Assert.Equal(expectedStatPoints, result.Progression.StatPoints);
    }

    [Fact]
    public void ClampsToLevel145ExperienceEnd()
    {
        var progression = new CharacterProgression { Level = 144, Experience = 99 };
        IReadOnlyDictionary<uint, ExperienceDefinition> experience = Records(
            Record(144, 0, 50),
            Record(145, 50, 100, skillPoints: 1));

        LegacyExperienceAward result = LegacyExperienceAwarder.Apply(progression, 1, experience);

        Assert.Equal(1, result.LevelsGained);
        Assert.Equal(145, result.Progression.Level);
        Assert.Equal(100, result.Progression.Experience);
        Assert.Equal(1, result.Progression.SkillPoints);
        Assert.Equal(30, result.Progression.StatPoints);
    }

    [Fact]
    public void MissingNextLevelRecordPreservesLegacyLevelIncrementBehavior()
    {
        var progression = new CharacterProgression { Level = 5, Experience = 90 };
        IReadOnlyDictionary<uint, ExperienceDefinition> experience = Records(
            Record(5, 0, 50),
            Record(145, 10_000, 20_000));

        LegacyExperienceAward result = LegacyExperienceAwarder.Apply(progression, 1, experience);

        Assert.Equal(1, result.LevelsGained);
        Assert.Equal(6, result.Progression.Level);
        Assert.Equal(0, result.Progression.SkillPoints);
        Assert.Equal(0, result.Progression.StatPoints);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void IgnoresNonPositiveAwards(int amount)
    {
        var progression = new CharacterProgression { Level = 1, Experience = 20 };

        LegacyExperienceAward result = LegacyExperienceAwarder.Apply(
            progression,
            amount,
            Records(Record(1, 0, 100), Record(145, 10_000, 20_000)));

        Assert.False(result.WasApplied);
        Assert.Same(progression, result.Progression);
        Assert.Equal(0, result.LevelsGained);
    }

    [Fact]
    public void IgnoresCharactersAlreadyAtTheLevel145ExperienceEnd()
    {
        var progression = new CharacterProgression { Level = 145, Experience = 20_000 };

        LegacyExperienceAward result = LegacyExperienceAwarder.Apply(
            progression,
            100,
            Records(Record(145, 10_000, 20_000)));

        Assert.False(result.WasApplied);
        Assert.Same(progression, result.Progression);
    }

    [Fact]
    public void RequiresTheLegacyMaximumLevelRecordWhenCurrentLevelExists()
    {
        var progression = new CharacterProgression { Level = 1, Experience = 20 };

        Assert.Throws<InvalidOperationException>(() =>
            LegacyExperienceAwarder.Apply(progression, 10, Records(Record(1, 0, 100))));
    }

    private static Dictionary<uint, ExperienceDefinition> Records(
        params ExperienceDefinition[] records)
        => records.ToDictionary(record => record.Level);

    private static ExperienceDefinition Record(
        uint level,
        uint start,
        uint end,
        uint skillPoints = 0)
        => new()
        {
            Level = level,
            ExperienceStart = start,
            ExperienceEnd = end,
            SkillPoint = skillPoints,
        };
}
