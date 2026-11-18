using InfiniteSky.Protocol.Binary;

namespace InfiniteSky.Protocol.Tests;

public sealed class BinaryPrimitiveTests
{
    [Fact]
    public void LittleEndianIntegersRoundTripBoundaries()
    {
        var writer = new LegacyBinaryWriter();
        writer.WriteByte(byte.MaxValue);
        writer.WriteSByte(sbyte.MinValue);
        writer.WriteUInt16(ushort.MaxValue);
        writer.WriteInt16(short.MinValue);
        writer.WriteUInt32(uint.MaxValue);
        writer.WriteInt32(int.MinValue);

        Assert.Equal(
            new byte[]
            {
                0xff,
                0x80,
                0xff, 0xff,
                0x00, 0x80,
                0xff, 0xff, 0xff, 0xff,
                0x00, 0x00, 0x00, 0x80,
            },
            writer.ToArray());

        var reader = new LegacyBinaryReader(writer.WrittenMemory);
        Assert.Equal(byte.MaxValue, reader.ReadByte());
        Assert.Equal(sbyte.MinValue, reader.ReadSByte());
        Assert.Equal(ushort.MaxValue, reader.ReadUInt16());
        Assert.Equal(short.MinValue, reader.ReadInt16());
        Assert.Equal(uint.MaxValue, reader.ReadUInt32());
        Assert.Equal(int.MinValue, reader.ReadInt32());
    }

    [Fact]
    public void FloatUsesStandardSingleLittleEndian()
    {
        var writer = new LegacyBinaryWriter();
        writer.WriteFloat32(-630.25f);

        Assert.Equal([0x00, 0x90, 0x1D, 0xC4], writer.ToArray());
        Assert.Equal(-630.25f, new LegacyBinaryReader(writer.WrittenMemory).ReadFloat32());
    }

    [Fact]
    public void FixedStringStopsAtFirstNulAndPreservesPadding()
    {
        var writer = new LegacyBinaryWriter();
        int used = writer.WriteFixedString("AB".AsSpan(), 8);

        Assert.Equal(2, used);
        Assert.Equal([0x41, 0x42, 0, 0, 0, 0, 0, 0], writer.ToArray());
        Assert.Equal("AB", new LegacyBinaryReader(writer.WrittenMemory).ReadFixedString(8));
    }

    [Fact]
    public void FixedStringCompatibilityEncodingTruncatesAndReadsLatin1Bytes()
    {
        var writer = new LegacyBinaryWriter();
        int used = writer.WriteFixedString("A\u00FFB".AsSpan(), 2);

        Assert.Equal(2, used);
        Assert.Equal([0x41, 0xFF], writer.ToArray());
        Assert.Equal("A\u00FF", new LegacyBinaryReader(writer.WrittenMemory).ReadFixedString(2));
    }

    [Fact]
    public void NegativeAndZeroLengthsFailBeforeAccess()
    {
        var reader = new LegacyBinaryReader(Array.Empty<byte>());

        var exception = Assert.Throws<LegacyProtocolReadException>(() => reader.ReadUInt32());
        Assert.Equal(0, exception.Offset);
        Assert.Equal(4, exception.Requested);
        Assert.Equal(0, exception.Remaining);
    }

    [Fact]
    public void ReaderOffsetResetAndRemainingAreExact()
    {
        var reader = new LegacyBinaryReader(new byte[] { 1, 2, 3, 4 });

        Assert.Equal(1, reader.ReadByte());
        Assert.Equal(3, reader.Remaining);
        reader.Skip(2);
        Assert.Equal(1, reader.Remaining);
        reader.Reset(0);
        Assert.Equal(0, reader.Offset);
    }

    [Fact]
    public void ScalarLegacyRestructFloatAddsOneToBitPattern()
    {
        var writer = new LegacyBinaryWriter();
        LegacyRestructFloat.WriteScalarBugged(writer, -630.25f);

        Assert.Equal([0x01, 0x90, 0x1D, 0xC4], writer.ToArray());

        var reader = new LegacyBinaryReader(writer.WrittenMemory);
        float decoded = LegacyRestructFloat.ReadScalar(reader);
        Assert.Equal(-630.25006103515625f, decoded);
    }
}
