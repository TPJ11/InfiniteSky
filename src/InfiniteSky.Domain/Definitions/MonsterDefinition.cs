using InfiniteSky.Domain.Identifiers;

namespace InfiniteSky.Domain.Definitions;

/// <summary>
/// Named monster fields explicitly identified by the original information reader.
/// </summary>
public sealed record MonsterDefinition
{
    public required MonsterDefinitionId Id { get; init; }
    public required string Name { get; init; }
    public uint SpecialModelId { get; init; }
    public uint ModelId { get; init; }
    public int ClickWidth { get; init; }
    public int ClickHeight { get; init; }
    public int ClickDepth { get; init; }
    public uint Level { get; init; }
    public uint Health { get; init; }
    public int Radius1 { get; init; }
    public int Radius2 { get; init; }
    public int WalkSpeed { get; init; }
    public int RunSpeed { get; init; }
    public int DeathSpeed { get; init; }
    public int AttackPower { get; init; }
    public int DefensePower { get; init; }
    public int AttackSuccess { get; init; }
    public int AttackBlock { get; init; }
    public int ElementAttackPower { get; init; }
    public int ElementDefensePower { get; init; }
    public int Critical { get; init; }
    public int LightAttack { get; init; }
    public int ShadowAttack { get; init; }
    public int DarkAttack { get; init; }
    public int ImproveStone1Chance { get; init; }
    public int ImproveStone1Id { get; init; }
    public int ImproveStone2Chance { get; init; }
    public int ImproveStone2Id { get; init; }
    public int PetDropChance { get; init; }
    public int PetId1 { get; init; }
    public int PetId2 { get; init; }
}
