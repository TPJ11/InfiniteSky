namespace InfiniteSky.Networking;

public interface IConnectionManager
{
    int Count { get; }
    bool TryAdd(NetworkConnection connection);
    bool TryGet(NetworkConnectionId id, out NetworkConnection? connection);
    bool TryRemove(NetworkConnectionId id);
    IReadOnlyCollection<NetworkConnection> Connections { get; }
}
