using System.Buffers;
using System.Buffers.Binary;

namespace InfiniteSky.Protocol.Binary;

/// <summary>
/// Little-endian writer that exposes bounded spans and exact written offsets.
/// </summary>
public sealed class LegacyBinaryWriter
{
    private readonly ArrayBufferWriter<byte> _buffer = new();

    public int Offset => _buffer.WrittenCount;
    public ReadOnlySpan<byte> WrittenSpan => _buffer.WrittenSpan;
    public ReadOnlyMemory<byte> WrittenMemory => _buffer.WrittenMemory;

    public Span<byte> GetSpan(int sizeHint = 0) => _buffer.GetSpan(sizeHint);

    public void Advance(int count)
    {
        if (count < 0)
        {
            throw new LegacyProtocolWriteException("Cannot advance a negative number of bytes.");
        }
        _buffer.Advance(count);
    }

    public void WriteByte(byte value)
    {
        Span<byte> destination = _buffer.GetSpan(1);
        destination[0] = value;
        _buffer.Advance(1);
    }

    public void WriteSByte(sbyte value) => WriteByte(unchecked((byte)value));

    public void WriteUInt16(ushort value)
    {
        Span<byte> destination = _buffer.GetSpan(sizeof(ushort));
        BinaryPrimitives.WriteUInt16LittleEndian(destination, value);
        _buffer.Advance(sizeof(ushort));
    }

    public void WriteInt16(short value)
    {
        Span<byte> destination = _buffer.GetSpan(sizeof(short));
        BinaryPrimitives.WriteInt16LittleEndian(destination, value);
        _buffer.Advance(sizeof(short));
    }

    public void WriteUInt32(uint value)
    {
        Span<byte> destination = _buffer.GetSpan(sizeof(uint));
        BinaryPrimitives.WriteUInt32LittleEndian(destination, value);
        _buffer.Advance(sizeof(uint));
    }

    public void WriteInt32(int value)
    {
        Span<byte> destination = _buffer.GetSpan(sizeof(int));
        BinaryPrimitives.WriteInt32LittleEndian(destination, value);
        _buffer.Advance(sizeof(int));
    }

    public void WriteFloat32(float value)
    {
        Span<byte> destination = _buffer.GetSpan(sizeof(float));
        BinaryPrimitives.WriteSingleLittleEndian(destination, value);
        _buffer.Advance(sizeof(float));
    }

    public void WriteBytes(ReadOnlySpan<byte> value)
    {
        Span<byte> destination = _buffer.GetSpan(value.Length);
        value.CopyTo(destination);
        _buffer.Advance(value.Length);
    }

    public int WriteFixedString(ReadOnlySpan<char> value, int width)
        => LegacyFixedString.Write(this, value, width);

    public byte[] ToArray() => _buffer.WrittenSpan.ToArray();

    public void Clear() => _buffer.ResetWrittenCount();
}
