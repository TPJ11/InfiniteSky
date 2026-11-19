namespace InfiniteSky.Networking;

public sealed record LegacyProtocolFrame(
    byte[] Header,
    byte Opcode,
    byte[] Payload)
{
    public int WireSize => Header.Length + Payload.Length;
}
