using System.Globalization;

using InfiniteSky.Protocol.Binary;
using InfiniteSky.Protocol.Frames;
using InfiniteSky.Protocol.Packets;
using InfiniteSky.Protocol.Structures;

namespace InfiniteSky.Protocol.Tests;

public sealed class FrameAndStructureTests
{
    [Fact]
    public void ReceiveFrameReadsOpcodeAtOffsetEight()
    {
        Span<byte> input = stackalloc byte[12];
        input.Clear();
        input[8] = 0x03;

        Assert.True(LegacyReceiveFrame.TryParse(input, 3, out byte opcode, out ReadOnlySpan<byte> payload, out int consumed));
        Assert.Equal((byte)0x03, opcode);
        Assert.Equal(3, payload.Length);
        Assert.Equal(12, consumed);
    }

    [Fact]
    public void ReceiveFrameRejectsIncompleteHeaderAndNegativeSize()
    {
        Assert.False(LegacyReceiveFrame.TryParse(stackalloc byte[8], 1, out _, out _, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => LegacyReceiveFrame.TotalSize(-1));
    }

    [Fact]
    public void PacketRegistryRejectsDuplicateOpcodes()
    {
        var registry = new LegacyPacketRegistry();
        registry.Add(new LegacyPacketDefinition(0x03, "Login", 91));

        Assert.Equal("Login", registry.Get(0x03).Name);
        Assert.Throws<ArgumentException>(() => registry.Add(new LegacyPacketDefinition(0x03, "Duplicate", 1)));
    }

    [Fact]
    public void Cvec3EncodesWithOriginalScalarFloatBug()
    {
        var writer = new LegacyBinaryWriter();
        var value = new LegacyVector3(-630.25f, 50.5f, -815.75f);
        value.Encode(writer);

        Assert.Equal(
            "01901d c40100 4a42 01f0 4bc4".Replace(" ", string.Empty, StringComparison.Ordinal),
            Convert.ToHexString(writer.ToArray()).ToLowerInvariant());

        var reader = new LegacyBinaryReader(writer.WrittenMemory);
        LegacyVector3 decoded = LegacyVector3.Decode(reader);
        Assert.Equal(-630.25006103515625f, decoded.X);
        Assert.Equal(50.500003814697266f, decoded.Y);
        Assert.Equal(-815.75006103515625f, decoded.Z);
    }

    [Fact]
    public void StandardCvec3FixtureUsesNormalFloatBits()
    {
        var writer = new LegacyBinaryWriter();
        var value = new LegacyStandardVector3(-630.25f, 50.5f, -815.75f);
        value.Encode(writer);

        Assert.Equal([0x00, 0x90, 0x1D, 0xC4, 0x00, 0x00, 0x4A, 0x42, 0x00, 0xF0, 0x4B, 0xC4], writer.ToArray());
    }

    [Fact]
    public void StorageItemRoundTripsExactWireSize()
    {
        var value = new LegacyStorageItem(42, 3, 7, 99, 5, 2, 0x1234);
        var writer = new LegacyBinaryWriter();
        value.Encode(writer);

        Assert.Equal(LegacyStorageItem.WireSize, writer.Offset);
        Assert.Equal(
            "2A00000003000000070000006300000005023412",
            Convert.ToHexString(writer.ToArray()));

        LegacyStorageItem decoded = LegacyStorageItem.Decode(new LegacyBinaryReader(writer.WrittenMemory));
        Assert.Equal(value, decoded);
    }

    [Fact]
    public void SmallStorageEquipmentAndGiftStructuresRoundTrip()
    {
        var equipmentWriter = new LegacyBinaryWriter();
        var equipment = new LegacyEquipment(7, 100, 3, 2, 0x1234);
        equipment.Encode(equipmentWriter);
        Assert.Equal(LegacyEquipment.WireSize, equipmentWriter.Offset);
        Assert.Equal(equipment, LegacyEquipment.Decode(new LegacyBinaryReader(equipmentWriter.WrittenMemory)));

        var smallWriter = new LegacyBinaryWriter();
        var small = new LegacySmallStorageItem(2, 99, 1, 2, 3);
        small.Encode(smallWriter);
        Assert.Equal(LegacySmallStorageItem.WireSize, smallWriter.Offset);
        Assert.Equal(small, LegacySmallStorageItem.Decode(new LegacyBinaryReader(smallWriter.WrittenMemory)));

        var giftWriter = new LegacyBinaryWriter();
        var gift = new LegacyGiftItem(55, 9);
        gift.Encode(giftWriter);
        Assert.Equal(LegacyGiftItem.WireSize, giftWriter.Offset);
        Assert.Equal(gift, LegacyGiftItem.Decode(new LegacyBinaryReader(giftWriter.WrittenMemory)));
    }

    [Fact]
    public void WorldRegionRoundTripsSignedAndUnsignedLookingFields()
    {
        var value = new LegacyWorldRegion(0, 6, 1, -1, -100, 50, 300, 25);
        var writer = new LegacyBinaryWriter();
        value.Encode(writer);

        Assert.Equal(LegacyWorldRegion.WireSize, writer.Offset);
        Assert.Equal(value, LegacyWorldRegion.Decode(new LegacyBinaryReader(writer.WrittenMemory)));
    }

    [Fact]
    public void AttackReplyPreservesExact56ByteLayout()
    {
        var value = new LegacyAttackPacketReply(
            0, 10, 2, 20, 3, 5, 6, 7, 8, 1, 100,
            1, 123, 239, 243, -32);
        var writer = new LegacyBinaryWriter();
        value.Encode(writer);

        Assert.Equal(LegacyAttackPacketReply.WireSize, writer.Offset);
        Assert.Equal(value, LegacyAttackPacketReply.Decode(new LegacyBinaryReader(writer.WrittenMemory)));
    }

    [Fact]
    public void LoginGreetingMatchesOriginal21ByteNodeFixture()
    {
        byte[] fixture = FixtureLoader.ReadHex("login-greeting.hex.txt");
        var greeting = LegacyLoginGreeting.Decode(new LegacyBinaryReader(fixture));

        Assert.Equal((byte)0, greeting.Unknown);
        Assert.Equal((uint)1000, greeting.PlayerCount);

        var writer = new LegacyBinaryWriter();
        greeting.Encode(writer);
        Assert.Equal(fixture, writer.ToArray());
    }

    [Fact]
    public void MapLoadReplyAndUncompressedEnvelopeRoundTrip()
    {
        var writer = new LegacyBinaryWriter();
        var reply = new LegacyMapLoadReply(0x0A, 0, "127.0.0.1", 11092);
        reply.Encode(writer);

        Assert.Equal(LegacyMapLoadReply.WireSize, writer.Offset);
        Assert.Equal(reply, LegacyMapLoadReply.Decode(new LegacyBinaryReader(writer.WrittenMemory)));

        var envelopeWriter = new LegacyBinaryWriter();
        var envelope = new LegacyCompressedPacketEnvelope(0x18, false);
        envelope.Encode(envelopeWriter);
        Assert.Equal([0x18, 0x00], envelopeWriter.ToArray());
    }

    [Fact]
    public void SpawnAndQuickUseStructuresPreserveSizes()
    {
        var spawn = new LegacySpawnInfo(
            9,
            100,
            new LegacyStandardVector3(-1.5f, 2.5f, 3.5f),
            359.5f);
        var writer = new LegacyBinaryWriter();
        spawn.Encode(writer);
        Assert.Equal(LegacySpawnInfo.WireSize, writer.Offset);

        var quickItem = new LegacyQuickUseItem(2, 99);
        Assert.Equal(LegacyQuickUseItem.WireSize, 8);
        Assert.Equal(2u, quickItem.Id);

        var quickSkill = new LegacyQuickUseSkill(1, 10);
        Assert.Equal(LegacyQuickUseSkill.WireSize, 8);
        Assert.Equal(10u, quickSkill.Level);
    }

    [Fact]
    public void LoginRequestMatchesOriginalNodeFixture()
    {
        byte[] fixture = FixtureLoader.ReadHex("login-request.hex.txt");
        var request = LegacyLoginRequest.Decode(new LegacyBinaryReader(fixture));

        Assert.Equal("PlayerName", request.Username);
        Assert.Equal("Secret123", request.Password);

        var writer = new LegacyBinaryWriter();
        request.Encode(writer);
        Assert.Equal(fixture, writer.ToArray());
    }

    [Fact]
    public void ProvenCoreRegistriesExposeFixedPacketSizes()
    {
        LegacyPacketRegistry login = LegacyCorePacketRegistry.CreateLogin();
        LegacyPacketRegistry world = LegacyCorePacketRegistry.CreateWorld();

        Assert.Equal(91, login.Get(0x03).PayloadSize);
        Assert.Equal(18, world.Get(0x01).PayloadSize);
        Assert.Equal(68, world.Get(0x14).PayloadSize);
        Assert.False(world.TryGet(0x02, out _));
    }

    private static class FixtureLoader
    {
        public static byte[] ReadHex(string name)
        {
            string path = Path.Combine(
                AppContext.BaseDirectory,
                "fixtures",
                "protocol",
                name);
            string text = File.ReadAllText(path);
            string[] parts = text.Split(
                (char[])[' ', '\r', '\n', '\t', '-'],
                StringSplitOptions.RemoveEmptyEntries);
            return parts.Select(part => byte.Parse(part, NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToArray();
        }
    }
}
