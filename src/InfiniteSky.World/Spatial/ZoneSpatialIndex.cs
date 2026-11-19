using System.Diagnostics;

using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Values;
using InfiniteSky.World.Zones;

namespace InfiniteSky.World.Spatial;

/// <summary>
/// Per-zone range QuadTree matching the legacy X/Z radius query and object-size semantics.
/// </summary>
public sealed class ZoneSpatialIndex : ISpatialIndex, IDisposable
{
    private const double RootX = -50_000;
    private const double RootZ = -50_000;
    private const double RootSize = 100_000;

    private readonly object _sync = new();
    private readonly Dictionary<EntityId, IndexedObject> _objects = [];
    private readonly Node _root;
    private readonly int _depth;
    private readonly int _nodeLimit;
    private readonly ZoneMetrics? _metrics;
    private uint _nextId = 1;
    private bool _disposed;

    public ZoneSpatialIndex(int depth = 6, int nodeLimit = 10, ZoneMetrics? metrics = null)
    {
        if (depth is < 0 or > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(depth));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(nodeLimit);
        _depth = depth;
        _nodeLimit = nodeLimit;
        _metrics = metrics;
        _root = new Node(RootX, RootZ, RootSize, 0);
    }

    public int Count
    {
        get
        {
            lock (_sync)
            {
                return _objects.Count;
            }
        }
    }

    public EntityId Add(Position3 position, string type, double size = 1)
    {
        ValidatePosition(position);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        size = NormalizeSize(size);

        lock (_sync)
        {
            ThrowIfDisposed();
            if (_nextId == 0)
            {
                throw new InvalidOperationException("The zone spatial node ID space has been exhausted.");
            }

            var entry = new IndexedObject(new EntityId(_nextId++), type, position, size);
            if (!Insert(_root, entry, isRoot: true))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(position),
                    "The object does not fit within the legacy QuadTree bounds.");
            }

            _objects.Add(entry.Id, entry);
            _metrics?.RecordSpatialObjectDelta(1);
            return entry.Id;
        }
    }

    public bool Update(EntityId id, Position3 position, double? size = null)
    {
        ValidatePosition(position);
        if (size.HasValue && (!double.IsFinite(size.Value) || size.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Spatial object size must be finite and non-negative.");
        }

        lock (_sync)
        {
            ThrowIfDisposed();
            if (!_objects.TryGetValue(id, out IndexedObject? entry))
            {
                return false;
            }

            Position3 oldPosition = entry.Position;
            double oldSize = entry.Size;
            double updatedSize = size is double requestedSize && requestedSize != 0 ? requestedSize : oldSize;
            entry.Partition!.Entries.Remove(entry);
            entry.Position = position;
            entry.Size = updatedSize;
            entry.Partition = null;
            if (Insert(_root, entry, isRoot: true))
            {
                return true;
            }

            entry.Position = oldPosition;
            entry.Size = oldSize;
            if (!Insert(_root, entry, isRoot: true))
            {
                throw new InvalidOperationException("The spatial index could not restore an object after a rejected update.");
            }

            throw new ArgumentOutOfRangeException(
                nameof(position),
                "The object does not fit within the legacy QuadTree bounds.");
        }
    }

    public bool Remove(EntityId id)
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            if (!_objects.Remove(id, out IndexedObject? entry))
            {
                return false;
            }

            entry.Partition!.Entries.Remove(entry);
            entry.Partition = null;
            _metrics?.RecordSpatialObjectDelta(-1);
            PruneEmptyChildren(_root);
            return true;
        }
    }

    public SpatialObjectSnapshot? Find(EntityId id)
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            return _objects.TryGetValue(id, out IndexedObject? entry)
                ? Snapshot(entry, null)
                : null;
        }
    }

    public IReadOnlyList<SpatialObjectSnapshot> Query(
        Position3 center,
        double radius,
        IReadOnlySet<string>? types = null,
        bool sortByDistance = false)
    {
        ValidatePosition(center);
        if (!double.IsFinite(radius) || radius < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius));
        }

        long started = Stopwatch.GetTimestamp();
        try
        {
            lock (_sync)
            {
                ThrowIfDisposed();
                var results = new List<SpatialObjectSnapshot>();
                Query(_root, center.X, center.Z, radius, types, results);
                if (sortByDistance)
                {
                    results.Sort(static (left, right) => Nullable.Compare(left.Distance, right.Distance));
                }

                return results.ToArray();
            }
        }
        finally
        {
            _metrics?.RecordSpatialQuery(Stopwatch.GetElapsedTime(started));
        }
    }

    public void Clear()
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            if (_objects.Count > 0)
            {
                _metrics?.RecordSpatialObjectDelta(-_objects.Count);
            }
            _objects.Clear();
            ResetNode(_root);
            _nextId = 1;
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            if (_objects.Count > 0)
            {
                _metrics?.RecordSpatialObjectDelta(-_objects.Count);
            }
            _objects.Clear();
            ResetNode(_root);
            _disposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private bool Insert(Node node, IndexedObject entry, bool isRoot)
    {
        if (node.Children is not null)
        {
            Node? child = FindContainingChild(node, entry);
            if (child is null)
            {
                node.Entries.Add(entry);
                entry.Partition = node;
            }
            else if (!Insert(child, entry, isRoot: false))
            {
                return false;
            }

            return true;
        }

        if (node.Depth < _depth && node.Entries.Count + 1 > _nodeLimit)
        {
            Split(node);
            node.Entries.Add(entry);
            entry.Partition = node;

            IndexedObject[] priorEntries = node.Entries.ToArray();
            node.Entries.Clear();
            foreach (IndexedObject priorEntry in priorEntries)
            {
                Node? child = FindContainingChild(node, priorEntry);
                if (child is null)
                {
                    node.Entries.Add(priorEntry);
                    priorEntry.Partition = node;
                }
                else if (!Insert(child, priorEntry, isRoot: false))
                {
                    return false;
                }
            }

            return true;
        }

        if (!isRoot && !node.InBounds(entry))
        {
            return false;
        }

        if (isRoot && node.Children is null && !node.InBounds(entry))
        {
            return false;
        }

        node.Entries.Add(entry);
        entry.Partition = node;
        return true;
    }

    private static void Query(
        Node node,
        double x,
        double z,
        double radius,
        IReadOnlySet<string>? types,
        List<SpatialObjectSnapshot> results)
    {
        if (node.Children is not null)
        {
            foreach (Node child in node.Children)
            {
                if (IntersectsStrict(x, z, radius, child))
                {
                    Query(child, x, z, radius, types, results);
                }
            }
        }

        foreach (IndexedObject entry in node.Entries)
        {
            if (types is not null && !types.Contains(entry.Type))
            {
                continue;
            }

            double dx = entry.Position.X - x;
            double dz = entry.Position.Z - z;
            double distance = Math.Sqrt(Math.Abs((dx * dx) + (dz * dz))) - entry.Size;
            if (distance <= radius)
            {
                results.Add(Snapshot(entry, distance));
            }
        }
    }

    private static void Split(Node node)
    {
        double halfSize = node.Size / 2;
        int childDepth = node.Depth + 1;
        node.Children =
        [
            new Node(node.X, node.Z, halfSize, childDepth),
            new Node(node.X + halfSize, node.Z, halfSize, childDepth),
            new Node(node.X, node.Z + halfSize, halfSize, childDepth),
            new Node(node.X + halfSize, node.Z + halfSize, halfSize, childDepth),
        ];
    }

    private static Node? FindContainingChild(Node node, IndexedObject entry)
    {
        if (node.Children is null)
        {
            return null;
        }

        foreach (Node child in node.Children)
        {
            if (child.InBounds(entry))
            {
                return child;
            }
        }

        return null;
    }

    private static bool IntersectsStrict(double x, double z, double radius, Node node)
    {
        double nearestX = Math.Clamp(x, node.X, node.X + node.Size);
        double nearestZ = Math.Clamp(z, node.Z, node.Z + node.Size);
        double dx = nearestX - x;
        double dz = nearestZ - z;
        double distance = Math.Sqrt(Math.Abs((dx * dx) + (dz * dz)));
        return distance < radius;
    }

    private static void PruneEmptyChildren(Node node)
    {
        if (node.Children is null)
        {
            return;
        }

        foreach (Node child in node.Children)
        {
            PruneEmptyChildren(child);
        }

        if (node.Entries.Count == 0
            && node.Children.All(static child => child.Entries.Count == 0 && child.Children is null))
        {
            node.Children = null;
        }
    }

    private static void ResetNode(Node node)
    {
        node.Entries.Clear();
        if (node.Children is null)
        {
            return;
        }

        foreach (Node child in node.Children)
        {
            ResetNode(child);
        }

        node.Children = null;
    }

    private static SpatialObjectSnapshot Snapshot(IndexedObject entry, double? distance)
        => new(entry.Id, entry.Type, entry.Position, entry.Size, distance);

    private static void ValidatePosition(Position3 position)
    {
        if (!double.IsFinite(position.X) || !double.IsFinite(position.Y) || !double.IsFinite(position.Z))
        {
            throw new ArgumentOutOfRangeException(nameof(position), "Spatial coordinates must be finite.");
        }
    }

    private static double NormalizeSize(double size)
    {
        if (!double.IsFinite(size) || size < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Spatial object size must be finite and non-negative.");
        }

        return size == 0 ? 1 : size;
    }

    private sealed class Node(double x, double z, double size, int depth)
    {
        public double X { get; } = x;
        public double Z { get; } = z;
        public double Size { get; set; } = size;
        public int Depth { get; } = depth;
        public List<IndexedObject> Entries { get; } = [];
        public Node[]? Children { get; set; }

        public bool InBounds(IndexedObject entry)
            => X < entry.Position.X - entry.Size
                && X + Size > entry.Position.X + entry.Size
                && Z < entry.Position.Z - entry.Size
                && Z + Size > entry.Position.Z + entry.Size;
    }

    private sealed class IndexedObject(EntityId id, string type, Position3 position, double size)
    {
        public EntityId Id { get; } = id;
        public string Type { get; } = type;
        public Position3 Position { get; set; } = position;
        public double Size { get; set; } = size;
        public Node? Partition { get; set; }
    }
}
