using System.Collections.ObjectModel;

namespace InfiniteSky.Networking;

public sealed class ConnectionManager : IConnectionManager
{
    private readonly object _sync = new();
    private readonly Dictionary<NetworkConnectionId, NetworkConnection> _connections =
        new();
    private readonly int _maximum;

    public ConnectionManager(int maximum)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximum);
        _maximum = maximum;
    }

    public int Count
    {
        get
        {
            lock (_sync)
            {
                return _connections.Count;
            }
        }
    }

    public IReadOnlyCollection<NetworkConnection> Connections
    {
        get
        {
            lock (_sync)
            {
                return new ReadOnlyCollection<NetworkConnection>(
                    _connections.Values.ToArray());
            }
        }
    }

    public bool TryAdd(NetworkConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        lock (_sync)
        {
            if (_connections.Count >= _maximum)
            {
                return false;
            }
            return _connections.TryAdd(connection.Id, connection);
        }
    }

    public bool TryGet(NetworkConnectionId id, out NetworkConnection? connection)
    {
        lock (_sync)
        {
            return _connections.TryGetValue(id, out connection);
        }
    }

    public bool TryRemove(NetworkConnectionId id)
    {
        lock (_sync)
        {
            return _connections.Remove(id);
        }
    }
}
