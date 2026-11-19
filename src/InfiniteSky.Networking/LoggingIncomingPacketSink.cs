using Microsoft.Extensions.Logging;

namespace InfiniteSky.Networking;

/// <summary>
/// Explicitly reports frames for which no application handler exists.
/// It never represents an authenticated player or a completed game packet.
/// </summary>
public sealed partial class LoggingIncomingPacketSink : IIncomingPacketSink
{
    private readonly ILogger<LoggingIncomingPacketSink> _logger;

    public LoggingIncomingPacketSink(
        ILogger<LoggingIncomingPacketSink> logger)
    {
        _logger = logger;
    }

    public ValueTask OnFrameAsync(
        NetworkConnection connection,
        LegacyProtocolFrame frame,
        CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            string connectionId = connection.Id.ToString();
            LogUnhandledFrame(
                _logger,
                connectionId,
                frame.Opcode);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask OnUnknownOpcodeAsync(
        NetworkConnection connection,
        byte opcode,
        CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            string connectionId = connection.Id.ToString();
            LogUnknownOpcode(
                _logger,
                connectionId,
                opcode);
        }
        return ValueTask.CompletedTask;
    }

    [LoggerMessage(
        EventId = 3010,
        Level = LogLevel.Debug,
        Message = "Received unhandled protocol frame 0x{Opcode:X2} on connection {ConnectionId}.")]
    private static partial void LogUnhandledFrame(
        ILogger logger,
        string connectionId,
        byte opcode);

    [LoggerMessage(
        EventId = 3011,
        Level = LogLevel.Warning,
        Message = "Received unknown protocol opcode 0x{Opcode:X2} on connection {ConnectionId}.")]
    private static partial void LogUnknownOpcode(
        ILogger logger,
        string connectionId,
        byte opcode);
}
