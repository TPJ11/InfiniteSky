using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Values;
using InfiniteSky.Domain.Zones;
using InfiniteSky.Protocol.Binary;
using InfiniteSky.Protocol.Structures;

namespace InfiniteSky.GameData.Zones;

/// <summary>
/// Reads the original count-prefixed NPC and WREGION assets without interpreting inactive gameplay rules.
/// </summary>
public sealed class LegacyZoneResourceLoader(string dataDirectory) : IZoneResourceLoader
{
    private readonly string _dataDirectory = Path.GetFullPath(
        string.IsNullOrWhiteSpace(dataDirectory)
            ? throw new ArgumentException("A data directory is required.", nameof(dataDirectory))
            : dataDirectory);

    /// <inheritdoc />
    public async Task<ZoneResourceData> LoadAsync(ZoneId zoneId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(zoneId.Value);

        string paddedId = zoneId.Value.ToString("D3", System.Globalization.CultureInfo.InvariantCulture);
        (byte[]? npcBytes, bool hasNpcFile) = await TryReadAsync(
            Path.Combine(_dataDirectory, "spawninfo", paddedId + ".NPC"),
            cancellationToken).ConfigureAwait(false);
        (byte[]? moveBytes, bool hasMoveFile) = await TryReadAsync(
            Path.Combine(_dataDirectory, "world", $"Z{paddedId}_ZONEMOVEREGION.WREGION"),
            cancellationToken).ConfigureAwait(false);
        (byte[]? safeBytes, bool hasSafeFile) = await TryReadAsync(
            Path.Combine(_dataDirectory, "world", $"Z{paddedId}_ZONESAFEREGION.WREGION"),
            cancellationToken).ConfigureAwait(false);

        SpawnDefinition[] spawns = hasNpcFile
            ? ReadRecords(npcBytes!, LegacySpawnInfo.WireSize, LegacySpawnInfo.Decode)
                .Where(static spawn => spawn.Id != 0)
                .Select(static spawn => new SpawnDefinition(
                    spawn.UniqueId,
                    spawn.Id,
                    new Position3(spawn.Location.X, spawn.Location.Y, spawn.Location.Z),
                    spawn.Direction))
                .ToArray()
            : [];
        LegacyWorldRegion[] moveRegions = hasMoveFile
            ? ReadRecords(moveBytes!, LegacyWorldRegion.WireSize, LegacyWorldRegion.Decode)
            : [];
        LegacyWorldRegion[] safeRegions = hasSafeFile
            ? ReadRecords(safeBytes!, LegacyWorldRegion.WireSize, LegacyWorldRegion.Decode)
            : [];

        return new ZoneResourceData(
            Array.AsReadOnly(spawns),
            Array.AsReadOnly(moveRegions),
            Array.AsReadOnly(safeRegions),
            hasNpcFile,
            hasMoveFile,
            hasSafeFile);
    }

    private static async Task<(byte[]? Contents, bool Exists)> TryReadAsync(
        string path,
        CancellationToken cancellationToken)
    {
        try
        {
            return (await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false), true);
        }
        catch (FileNotFoundException)
        {
            return (null, false);
        }
        catch (DirectoryNotFoundException)
        {
            return (null, false);
        }
    }

    private static T[] ReadRecords<T>(
        byte[] contents,
        int recordSize,
        Func<LegacyBinaryReader, T> decode)
    {
        if (contents.Length < sizeof(uint))
        {
            throw new InvalidDataException("A legacy zone data file is missing its record-count header.");
        }

        var reader = new LegacyBinaryReader(contents);
        uint recordCount = reader.ReadUInt32();
        if (recordCount > (uint)(reader.Remaining / recordSize))
        {
            throw new InvalidDataException(
                $"A legacy zone data file declares {recordCount} records but does not contain {recordSize} bytes per record.");
        }

        var records = new T[recordCount];
        for (int index = 0; index < records.Length; index++)
        {
            records[index] = decode(reader);
        }

        return records;
    }
}
