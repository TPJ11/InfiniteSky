using System.Collections.Concurrent;
using System.Security.Cryptography;

using InfiniteSky.Application.Authentication;
using InfiniteSky.Networking;
using InfiniteSky.Protocol;
using InfiniteSky.Protocol.Binary;
using InfiniteSky.Protocol.Structures;

using Microsoft.Extensions.Logging;

namespace InfiniteSky.Server.Authentication;

/// <summary>
/// Decodes the login packet, invokes the application authentication service,
/// and maps application results to the legacy login reply.
/// </summary>
public sealed partial class AuthenticationIncomingPacketSink : IIncomingPacketSink
{
    private readonly IAuthenticationService _authentication;
    private readonly ILogger<AuthenticationIncomingPacketSink> _logger;
    private readonly ConcurrentDictionary<NetworkConnectionId, Action<NetworkConnection>>
        _closeHandlers = new();

    public AuthenticationIncomingPacketSink(
        IAuthenticationService authentication,
        ILogger<AuthenticationIncomingPacketSink> logger)
    {
        _authentication = authentication;
        _logger = logger;
    }

    public async ValueTask OnFrameAsync(
        NetworkConnection connection,
        LegacyProtocolFrame frame,
        CancellationToken cancellationToken)
    {
        if (connection.EndpointKind != NetworkEndpointKind.Login
            || frame.Opcode != LegacyLoginReply.PacketId)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                string connectionId = connection.Id.ToString();
                LogUnhandledFrame(_logger, connectionId, frame.Opcode);
            }
            return;
        }

        try
        {
            if (!EnsureCloseHandler(connection))
            {
                return;
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                string connectionId = connection.Id.ToString();
                LogRequestReceived(_logger, connectionId);
            }
            LegacyLoginRequest request =
                LegacyLoginRequest.Decode(new LegacyBinaryReader(frame.Payload));

            AuthenticationResult result = await _authentication.AuthenticateAsync(
                connection.Id.Value,
                request.Username,
                request.Password,
                cancellationToken);

            if (result.Outcome == AuthenticationOutcome.ConnectionClosed)
            {
                return;
            }

            LegacyLoginStatus status = ToLegacyStatus(result.Outcome);
            string? replyUsername = result.Outcome == AuthenticationOutcome.Authenticated
                ? result.Session?.Username
                    ?? throw new InvalidOperationException(
                        "A successful authentication result must contain its session.")
                : null;

            var replyWriter = new LegacyBinaryWriter();
            new LegacyLoginReply(status, replyUsername).Encode(replyWriter);
            byte[] reply = replyWriter.ToArray();

            if (_logger.IsEnabled(LogLevel.Information))
            {
                string connectionId = connection.Id.ToString();
                string sessionId = result.Session?.SessionId.ToString() ?? string.Empty;
                string outcome = result.Outcome.ToString();
                LogResult(
                    _logger,
                    connectionId,
                    sessionId,
                    outcome);
            }

            bool sent = await connection.SendAsync(reply, cancellationToken);
            if (!sent || ShouldClose(result.Outcome))
            {
                await connection.CloseAsync(CancellationToken.None);
            }
        }
        catch (LegacyProtocolReadException exception)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                string connectionId = connection.Id.ToString();
                LogMalformedRequest(_logger, connectionId, exception);
            }
            await SendCannotAuthenticateAndCloseAsync(connection, cancellationToken);
        }
        finally
        {
            if (frame.Opcode == LegacyLoginReply.PacketId)
            {
                CryptographicOperations.ZeroMemory(frame.Payload);
            }
        }
    }

    public ValueTask OnUnknownOpcodeAsync(
        NetworkConnection connection,
        byte opcode,
        CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            string connectionId = connection.Id.ToString();
            LogUnknownOpcode(_logger, connectionId, opcode);
        }
        return ValueTask.CompletedTask;
    }

    private bool EnsureCloseHandler(NetworkConnection connection)
    {
        if (connection.State is NetworkConnectionState.Closing
            or NetworkConnectionState.Closed
            or NetworkConnectionState.Faulted)
        {
            return false;
        }

        Action<NetworkConnection> handler = OnConnectionClosed;
        if (_closeHandlers.TryAdd(connection.Id, handler))
        {
            connection.Closed += handler;
        }

        if (connection.State is NetworkConnectionState.Closing
            or NetworkConnectionState.Closed
            or NetworkConnectionState.Faulted)
        {
            OnConnectionClosed(connection);
            return false;
        }

        return true;
    }

    private void OnConnectionClosed(NetworkConnection connection)
    {
        _authentication.ConnectionClosed(connection.Id.Value);
        if (_closeHandlers.TryRemove(connection.Id, out Action<NetworkConnection>? handler))
        {
            connection.Closed -= handler;
        }
    }

    private static LegacyLoginStatus ToLegacyStatus(AuthenticationOutcome outcome)
    {
        return outcome switch
        {
            AuthenticationOutcome.Authenticated => LegacyLoginStatus.Success,
            AuthenticationOutcome.AccountNotFound => LegacyLoginStatus.AccountNotFound,
            AuthenticationOutcome.IncorrectPassword => LegacyLoginStatus.IncorrectPassword,
            AuthenticationOutcome.AlreadyLoggedIn
                or AuthenticationOutcome.AlreadyAuthenticated => LegacyLoginStatus.AlreadyLoggedIn,
            _ => LegacyLoginStatus.CannotAuthenticate,
        };
    }

    private static bool ShouldClose(AuthenticationOutcome outcome)
    {
        return outcome is AuthenticationOutcome.AccountNotFound
            or AuthenticationOutcome.AlreadyLoggedIn
            or AuthenticationOutcome.CannotAuthenticate
            or AuthenticationOutcome.InvalidRequest
            or AuthenticationOutcome.TimedOut;
    }

    private static async Task SendCannotAuthenticateAndCloseAsync(
        NetworkConnection connection,
        CancellationToken cancellationToken)
    {
        var writer = new LegacyBinaryWriter();
        new LegacyLoginReply(LegacyLoginStatus.CannotAuthenticate).Encode(writer);
        await connection.SendAsync(writer.WrittenMemory, cancellationToken);
        await connection.CloseAsync(CancellationToken.None);
    }

    [LoggerMessage(
        EventId = 4000,
        Level = LogLevel.Information,
        Message = "Received a legacy login request on connection {ConnectionId}.")]
    private static partial void LogRequestReceived(ILogger logger, string connectionId);

    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Information,
        Message = "Authentication outcome {Outcome} for connection {ConnectionId}, session {SessionId}.")]
    private static partial void LogResult(
        ILogger logger,
        string connectionId,
        string sessionId,
        string outcome);

    [LoggerMessage(
        EventId = 4002,
        Level = LogLevel.Warning,
        Message = "Malformed legacy login request on connection {ConnectionId}.")]
    private static partial void LogMalformedRequest(
        ILogger logger,
        string connectionId,
        Exception exception);

    [LoggerMessage(
        EventId = 4003,
        Level = LogLevel.Debug,
        Message = "Unhandled login opcode 0x{Opcode:X2} on connection {ConnectionId}.")]
    private static partial void LogUnhandledFrame(
        ILogger logger,
        string connectionId,
        byte opcode);

    [LoggerMessage(
        EventId = 4004,
        Level = LogLevel.Warning,
        Message = "Unknown login opcode 0x{Opcode:X2} on connection {ConnectionId}.")]
    private static partial void LogUnknownOpcode(
        ILogger logger,
        string connectionId,
        byte opcode);
}
