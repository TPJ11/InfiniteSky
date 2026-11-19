# Spatial Systems

## Source algorithm and coordinates

The original zone owns one range QuadTree with root origin `(-50000,-50000)` and size `100000`; configured depth and node limit default to 6 and 10 ([`zone.js`](../../zone.js#L533-L549)). The tree maps `CVec3.X` to its horizontal X axis and `CVec3.Z` to its horizontal Y axis. Vertical `CVec3.Y` is not used by QuadTree proximity queries ([`QuadTree.js`](../../QuadTree.js#L191-L218)).

The root QuadTree and all child regions use square bounds. A node is assigned to a child only when its full `position +/- size` extent fits strictly inside that child's bounds (`inBounds` uses `<` and `>`). The tree splits after the next insertion would make the local node count exceed its limit; the maximum child depth is configurable ([`QuadTree.js`](../../QuadTree.js#L422-L456), [`QuadTree.js`](../../QuadTree.js#L491-L568)). Zone nodes use size as a radius: clients use character level, while NPCs, monsters, and items normally use 1 ([`zone.js`](../../zone.js#L56-L64), [`zone.js`](../../zone.js#L230-L232), [`zone.js`](../../zone.js#L353-L354), [`zone.js`](../../zone.js#L450-L451)).

For a candidate at `(x,z)` with size `s`, the legacy distance is:

```text
sqrt(abs((x - centerX)^2 + (z - centerZ)^2)) - s
```

The candidate test is inclusive (`distance <= radius`). This is not the same as `CVec3.getDistance`, which uses all three coordinates ([`QuadTree.js`](../../QuadTree.js#L254-L267), [`generic/CVec3.js`](../../generic/CVec3.js#L58-L78)).

## C# index behavior

[`ZoneSpatialIndex`](../../src/InfiniteSky.World/Spatial/ZoneSpatialIndex.cs#L12) is a per-zone implementation behind `ISpatialIndex`. It uses the original root bounds, depth, node limit, X/Z projection, size-aware candidate distance, child order, and strict child-fit rule. `Add` returns a generated zone-local `EntityId`; `Update` retains the ID and can change position and size; `Remove`, `Find`, radius query, type filter, `Clear`, and disposal are supported. Size zero follows the source fallback (1 on insertion, unchanged on update); negative or nonfinite sizes/positions are rejected explicitly.

The original child-pruning helper tests circle-to-cell distance with strict `< radius`, while the per-node candidate test uses `<= radius`. Therefore a node exactly on a child cell's tangent can be omitted after a split even though its own candidate distance equals the radius. The C# implementation deliberately preserves and tests that observable edge case rather than silently changing the client-facing recipient set. Child pruning also does not expand by object size, so large objects near a child boundary can be pruned; no corrected broad phase is introduced.

## Concurrency and cost

Each index operation is protected by one zone-local lock. Mutations and queries are linearized; a query returns a copied array snapshot and never exposes the mutable node lists. Registry enumeration is similarly copied and sorted by numeric zone ID. No lock is held across file, database, or network I/O.

The index is bounded to the configured depth (default 6). Insert, remove, and update walk at most the tree depth plus local bucket work. A well-distributed radius query visits intersecting cells plus candidate nodes; worst-case work remains O(n) when objects cannot be partitioned or a query overlaps most cells. Query results allocate a list and result array; no performance benchmark was run, so no measured speedup is claimed. Query duration and spatial object counts are instrumented for future load testing.

There is no periodic index scan in this stage. Callers must explicitly update a node when its position or size changes. The legacy `Zone.step` invokes `QuadTree.update` to refresh object-derived positions and prune empty child regions ([`zone.js`](../../zone.js#L28-L30), [`QuadTree.js`](../../QuadTree.js#L319-L420)); no actor or zone simulation tick is added here.

## Boundaries

- Coordinates remain `Position3` X/Y/Z values without a map/world transform. Only X and Z participate in this index; units and map extents are not established by available assets.
- The QuadTree's fixed root is an index extent, not verified collision or navigation bounds.
- Query radius is supplied by the caller. `ZoneRuntime.QueryActionCandidates` is a convenience for the source-configured action distance, but no universal visibility radius is imposed on other systems.
- This is proximity candidate selection only. It does not model player/entity membership, visibility state, packet recipients, broadcasts, collision, or navigation.
