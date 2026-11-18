using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace InfiniteSky.Protocol.Binary;

/// <summary>
/// Bounds-checked little-endian reader for the original protocol primitives.
/// </summary>
public sealed class LegacyBinaryReader
{
    private readonly ReadOnlyMemory<byte> _memory;

    public LegacyBinaryReader(ReadOnlyMemory<byte> memory)
    {
        _memory = memory;
    }

    public int Offset { get; private set; }
    public int Length => _memory.Length;
    public int Remaining => _memory.Length - Offset;
    public ReadOnlySpan<byte> RemainingSpan => _memory.Span[Offset..];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<byte> ReadSpan(int length)
    {
        if (length < 0)
        {
            throw new LegacyProtocolReadException(Offset, length, Remaining);
        }

        if (length > Remaining)
        {
            throw new LegacyProtocolReadException(Offset, length, Remaining);
        }

        ReadOnlySpan<byte> span = _memory.Span.Slice(Offset, length);
        Offset += length;
        return span;
    }

    public byte ReadByte() => ReadSpan(1)[0];

    public sbyte ReadSByte() => unchecked((sbyte)ReadByte());

    public ushort ReadUInt16() => BinaryPrimitives.ReadUInt16LittleEndian(ReadSpan(sizeof(ushort)));
    public short ReadInt16() => BinaryPrimitives.ReadInt16LittleEndian(ReadSpan(sizeof(short)));
    public uint ReadUInt32() => BinaryPrimitives.ReadUInt32LittleEndian(ReadSpan(sizeof(uint)));
    public int ReadInt32() => BinaryPrimitives.ReadInt32LittleEndian(ReadSpan(sizeof(int)));

    public float ReadFloat32()
    {
        uint bits = ReadUInt32();
        return BitConverter.Int32BitsToSingle(unchecked((int)bits));
    }

    public void ReadBytes(Span<byte> destination)
    {
        ReadSpan(destination.Length).CopyTo(destination);
    }

    public byte[] ReadBytes(int length) => ReadSpan(length).ToArray();

    public string ReadFixedString(int length)
    {
        return LegacyFixedString.Read(ReadSpan(length));
    }

    public void Skip(int length) => _ = ReadSpan(length);

    public void Reset(int offset)
    {
        if ((uint)offset > (uint)_memory.Length)
        {
            throw new LegacyProtocolReadException(offset, 0, _memory.Length);
        }
        Offset = offset;
    }
}
