using System.Collections.ObjectModel;

namespace InfiniteSky.Protocol.Packets;

/// <summary>
/// Holds explicit opcode descriptors and rejects duplicate definitions.
/// </summary>
public sealed class LegacyPacketRegistry
{
    private readonly Dictionary<byte, LegacyPacketDefinition> _definitions =
        new();

    public void Add(LegacyPacketDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (!_definitions.TryAdd(definition.Opcode, definition))
        {
            throw new ArgumentException(
                $"Opcode 0x{definition.Opcode:X2} is already registered.",
                nameof(definition));
        }
    }

    public bool TryGet(byte opcode, out LegacyPacketDefinition? definition)
        => _definitions.TryGetValue(opcode, out definition);

    public LegacyPacketDefinition Get(byte opcode)
    {
        if (!_definitions.TryGetValue(opcode, out LegacyPacketDefinition? definition))
        {
            throw new KeyNotFoundException(
                $"No packet definition exists for opcode 0x{opcode:X2}.");
        }

        return definition;
    }

    public IReadOnlyDictionary<byte, LegacyPacketDefinition> Definitions =>
        new ReadOnlyDictionary<byte, LegacyPacketDefinition>(_definitions);
}
