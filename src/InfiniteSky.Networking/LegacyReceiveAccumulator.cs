using InfiniteSky.Protocol.Packets;

namespace InfiniteSky.Networking;

/// <summary>
/// Bounded accumulator for 9-byte receive headers and fixed payload packets.
/// Unknown opcodes discard the header and report the opcode to the caller.
/// </summary>
public sealed class LegacyReceiveAccumulator
{
    private readonly byte[] _buffer;
    private readonly LegacyPacketRegistry _registry;
    private readonly int _maximumFrameBytes;
    private int _count;

    public LegacyReceiveAccumulator(
        int maximumBytes,
        int maximumFrameBytes,
        LegacyPacketRegistry registry)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumFrameBytes, 9);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maximumFrameBytes, maximumBytes);
        ArgumentNullException.ThrowIfNull(registry);
        _buffer = new byte[maximumBytes];
        _maximumFrameBytes = maximumFrameBytes;
        _registry = registry;
    }

    public int Count => _count;

    public void Append(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length > _buffer.Length - _count)
        {
            throw new InvalidOperationException(
                $"Receive buffer limit of {_buffer.Length} bytes would be exceeded.");
        }

        bytes.CopyTo(_buffer.AsSpan(_count, bytes.Length));
        _count += bytes.Length;
    }

    public FrameReadResult TryReadFrame(out LegacyProtocolFrame? frame, out byte unknownOpcode)
    {
        frame = null;
        unknownOpcode = 0;

        if (_count < 9)
        {
            return FrameReadResult.Incomplete;
        }

        byte opcode = _buffer[8];
        if (!_registry.TryGet(opcode, out LegacyPacketDefinition? definition) || definition is null)
        {
            unknownOpcode = opcode;
            Discard(9);
            return FrameReadResult.UnknownOpcode;
        }

        int payloadSize = definition.Kind == LegacyPacketKind.HeaderOnly
            ? 0
            : definition.PayloadSize;

        int totalSize = 9 + payloadSize;
        if (totalSize > _maximumFrameBytes)
        {
            throw new InvalidDataException(
                $"Frame size of {totalSize} bytes exceeds the configured maximum of {_maximumFrameBytes} bytes.");
        }

        if (_count < totalSize)
        {
            return FrameReadResult.Incomplete;
        }

        byte[] header = _buffer.AsSpan(0, 9).ToArray();
        byte[] payload = _buffer.AsSpan(9, payloadSize).ToArray();
        Discard(totalSize);
        frame = new LegacyProtocolFrame(header, opcode, payload);
        return FrameReadResult.Complete;
    }

    public void Clear() => _count = 0;

    private void Discard(int length)
    {
        if (length >= _count)
        {
            _count = 0;
            return;
        }

        _buffer.AsSpan(length, _count - length).CopyTo(_buffer);
        _count -= length;
    }
}
