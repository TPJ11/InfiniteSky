using InfiniteSky.Domain.Characters;
using InfiniteSky.Domain.Definitions;

namespace InfiniteSky.Domain.Progression;

public readonly record struct LegacyExperienceAward(
    CharacterProgression Progression,
    int LevelsGained,
    bool WasApplied);

public static class LegacyExperienceAwarder
{
    private const uint MaximumLevel = 145;

    public static LegacyExperienceAward Apply(
        CharacterProgression progression,
        long amount,
        IReadOnlyDictionary<uint, ExperienceDefinition> experienceByLevel)
    {
        ArgumentNullException.ThrowIfNull(progression);
        ArgumentNullException.ThrowIfNull(experienceByLevel);

        if (amount <= 0
            || progression.Level < 0
            || !experienceByLevel.TryGetValue((uint)progression.Level, out ExperienceDefinition? levelInfo))
        {
            return new LegacyExperienceAward(progression, 0, false);
        }

        if (!experienceByLevel.TryGetValue(MaximumLevel, out ExperienceDefinition? maximumLevelInfo))
        {
            throw new InvalidOperationException("The legacy level 145 experience record is required.");
        }

        if (maximumLevelInfo.ExperienceEnd == progression.Experience)
        {
            return new LegacyExperienceAward(progression, 0, false);
        }

        long experience = (long)Math.Min(
            (decimal)progression.Experience + amount,
            maximumLevelInfo.ExperienceEnd);
        long reminder = (long)levelInfo.ExperienceEnd - experience;
        int levelsGained = 0;
        int skillPoints = progression.SkillPoints;
        int statPoints = progression.StatPoints;

        while (reminder < 0)
        {
            levelsGained++;
            int nextLevel = progression.Level + levelsGained;
            if (nextLevel < 0
                || !experienceByLevel.TryGetValue((uint)nextLevel, out ExperienceDefinition? nextLevelInfo))
            {
                break;
            }

            experience++;
            skillPoints = checked(skillPoints + checked((int)nextLevelInfo.SkillPoint));
            statPoints = checked(statPoints + StatPointsForLevel(nextLevel));
            reminder = ((long)nextLevelInfo.ExperienceEnd - nextLevelInfo.ExperienceStart) + reminder;
        }

        int resultingLevel = progression.Level + levelsGained;
        if (resultingLevel > MaximumLevel || experience > maximumLevelInfo.ExperienceEnd)
        {
            levelsGained = (int)MaximumLevel - progression.Level;
            experience = maximumLevelInfo.ExperienceEnd;
            resultingLevel = (int)MaximumLevel;
        }

        return new LegacyExperienceAward(
            progression with
            {
                Level = resultingLevel,
                Experience = experience,
                SkillPoints = skillPoints,
                StatPoints = statPoints,
            },
            levelsGained,
            true);
    }

    private static int StatPointsForLevel(int level)
        => level > 99 && level <= 112 ? 0 : level > 112 ? 30 : 5;
}
