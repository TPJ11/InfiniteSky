# Skills and Progression

## Experience award source

The active root `world.js` `socket.giveEXP(value)`:

1. Ignores non-positive values.
2. Returns when the current level record is absent or current experience equals level 145's `EXPEnd`.
3. Adds the award and caps experience at the level-145 end.
4. Uses current-level `EXPEnd - Experience` as a remainder. While negative, it increments the gained-level count before resolving the next level record. If the record exists, it adds one experience, that record's `SkillPoint`, and stat points based on the new level: 5 through 99, 0 for 100-112, and 30 above 112. It advances the remainder by `next.EXPEnd - next.EXPStart`.
5. Sends the 0x2F update before applying the level change. If the resulting level exceeds 145 or experience exceeds the cap, it sets level/experience to 145/the cap.
6. On a level gain, refills current and persisted HP/Chi from the *pre-update* max values, updates state level, recalculates stats, broadcasts 0x2E level-up, then saves.

The extra experience increment per crossed level and the level counter increment when the next record is missing are source behavior, not inferred rules. `generic/Info_Exp.js` defines a 27-field record loaded from external `005_00001.IMG`; it includes two unnamed values that are not needed by this calculation. That file is not present in the repository's `data` tree.

The current .NET `ExperienceDefinition` maps the 25 named fields used by the loader and does not retain those two unknown values. `CharacterProgression.Experience` is a `long` and the award API accepts integral `long` values, whereas JavaScript uses `Number`; the actual source data and command input ranges are unavailable to establish parity outside the expected integral in-game range. Point/attribute arithmetic is checked against the current .NET `int` model rather than silently wrapping.

## C# slice

`InfiniteSky.Domain.Progression.LegacyExperienceAwarder` applies the arithmetic to a `CharacterProgression` using caller-supplied `ExperienceDefinition` records. Tests cover below-threshold awards, the exact source crossing behavior, stat-point bands, max-level cap, missing next-level record, non-positive input, and already-capped experience. `LegacyStatPointAllocator` also maps stat keys 0-7, applies the source one/five point allocation (ignoring the request's amount for valid keys), and returns statuses for no/insufficient points and invalid keys. Tests use synthetic values and are not asserted to represent actual game data.

These calculations are not connected to player runtime, damage, XP/stat persistence, derived stat recalculation, 0x2F, 0x2E, or 0x84. Those responsibilities remain blocked by missing loaders, stat behavior, packet codecs and save ownership. GP-004 and GP-005 are `IN_PROGRESS`, not `IMPLEMENTED` or `VERIFIED`.

## Stats and skill source status

`generic/statcalculation.js` has an active `characterStatsInfoObj` which derives values from clan, level experience data, equipped item modifiers and pet data. Its `updateLevel` calls `updateAll`; its `updateSkills` is empty. The later `generic.calculateCharacterinfos` implementation is labeled deprecated and its body is commented. Missing source images prevent representative calculations.

`packets/world/statpoints.js` handles keys 0-3 as one point and keys 4-7 as five points, applies the corresponding stat update, subtracts points, responds with opcode 0x84/key 0x37 and saves. A stat reset has a separate packet path and incomplete validation.

`packets/world/skillUse.js` includes 0x19 skill-state handling and 0x18 modifier interpolation into hard-coded buff slots. Some values are rounded, some are not; its 0x7A handler is empty. `itemactions.js` contains learn/upgrade/bar actions. There is no complete evidence for every skill effect, cooldown, duration, resource cost or expiration.

No derived stat, stat-reset, skill or buff gameplay has been migrated. GP-006 and GP-013..016 remain `NOT_STARTED`.
