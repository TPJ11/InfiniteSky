namespace InfiniteSky.Protocol.Packets;

/// <summary>
/// Proven fixed-size receive descriptors. Unknown/variable packet layouts are intentionally absent.
/// </summary>
public static class LegacyCorePacketRegistry
{
    public static LegacyPacketRegistry CreateLogin()
    {
        var registry = new LegacyPacketRegistry();
        registry.Add(new LegacyPacketDefinition(0x03, "Login", 91));
        registry.Add(new LegacyPacketDefinition(0x04, "KeepAlive", 8));
        registry.Add(new LegacyPacketDefinition(0x06, "CharacterCreate", 170));
        registry.Add(new LegacyPacketDefinition(0x07, "CharacterDelete", 21));
        registry.Add(new LegacyPacketDefinition(0x09, "GameStart", 13));
        registry.Add(new LegacyPacketDefinition(0x0A, "GameStartFailure", 9));
        registry.Add(new LegacyPacketDefinition(0x12, "PinChange", 18));
        registry.Add(new LegacyPacketDefinition(0x13, "PinAttempt", 13));
        return registry;
    }

    public static LegacyPacketRegistry CreateWorld()
    {
        var registry = new LegacyPacketRegistry();
        registry.Add(new LegacyPacketDefinition(0x01, "Handshake", 18));
        registry.Add(new LegacyPacketDefinition(0x08, "ZoneChange", 16));
        registry.Add(new LegacyPacketDefinition(0x14, "ItemAction", 68));
        registry.Add(new LegacyPacketDefinition(0x18, "ApplyBuff", 20));
        registry.Add(new LegacyPacketDefinition(0x19, "UsedSkillOrItem", 24));
        registry.Add(new LegacyPacketDefinition(0x7A, "BuffConfirmation", 62));
        return registry;
    }
}
