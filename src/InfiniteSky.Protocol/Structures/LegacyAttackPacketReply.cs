using InfiniteSky.Protocol.Binary;

namespace InfiniteSky.Protocol.Structures;

public readonly record struct LegacyAttackPacketReply(
    uint Action,
    uint AttackerId,
    uint AttackerIndex,
    uint DefenderId,
    uint DefenderIndex,
    uint A,
    uint B,
    uint C,
    uint D,
    uint Status,
    uint TotalDamage,
    ushort Deadly,
    ushort Light,
    ushort Shadow,
    ushort Dark,
    int DamageHp)
{
    public const int WireSize = 56;

    public static LegacyAttackPacketReply Decode(LegacyBinaryReader reader)
        => new(
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt16(),
            reader.ReadUInt16(),
            reader.ReadUInt16(),
            reader.ReadUInt16(),
            reader.ReadInt32());

    public void Encode(LegacyBinaryWriter writer)
    {
        writer.WriteUInt32(Action);
        writer.WriteUInt32(AttackerId);
        writer.WriteUInt32(AttackerIndex);
        writer.WriteUInt32(DefenderId);
        writer.WriteUInt32(DefenderIndex);
        writer.WriteUInt32(A);
        writer.WriteUInt32(B);
        writer.WriteUInt32(C);
        writer.WriteUInt32(D);
        writer.WriteUInt32(Status);
        writer.WriteUInt32(TotalDamage);
        writer.WriteUInt16(Deadly);
        writer.WriteUInt16(Light);
        writer.WriteUInt16(Shadow);
        writer.WriteUInt16(Dark);
        writer.WriteInt32(DamageHp);
    }
}
