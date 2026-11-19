using InfiniteSky.Domain.Identifiers;

namespace InfiniteSky.World.Zones;

/// <summary>
/// Stores only fully running zones and returns stable snapshots for enumeration.
/// </summary>
public sealed class ZoneRegistry : IZoneRegistry
{
    private readonly object _sync = new();
    private readonly Dictionary<ZoneId, ZoneRuntime> _zones = [];

    public bool TryRegister(ZoneRuntime zone)
    {
        ArgumentNullException.ThrowIfNull(zone);
        if (zone.State != ZoneLifecycleState.Running)
        {
            throw new InvalidOperationException("Only running zones may be registered.");
        }

        lock (_sync)
        {
            if (_zones.ContainsKey(zone.Descriptor.Id))
            {
                return false;
            }

            if (!zone.TryMarkRegistered())
            {
                throw new InvalidOperationException("The zone stopped while it was being registered.");
            }

            _zones.Add(zone.Descriptor.Id, zone);
            return true;
        }
    }

    public bool Unregister(ZoneId id)
    {
        lock (_sync)
        {
            if (!_zones.Remove(id, out ZoneRuntime? zone))
            {
                return false;
            }

            zone.MarkUnregistered();
            return true;
        }
    }

    public ZoneRuntime? Find(ZoneId id)
    {
        lock (_sync)
        {
            return _zones.GetValueOrDefault(id);
        }
    }

    public IReadOnlyList<ZoneRuntime> GetAll()
    {
        lock (_sync)
        {
            return Array.AsReadOnly(_zones.Values
                .OrderBy(static zone => zone.Descriptor.Id.Value)
                .ToArray());
        }
    }
}
