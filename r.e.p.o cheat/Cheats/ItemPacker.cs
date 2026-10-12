using System.Collections.Generic;
using UnityEngine;

namespace r.e.p.o_cheat
{
    // Heightmap-based 3D packing: each item is laid flat (smallest dimension vertical),
    // then dropped at the lowest well-supported spot of a grid over the zone.
    public static class ItemPacker
    {
        private const float Cell = 0.05f;
        private const float Gap = 0.04f;
        private const float DropHeight = 0.05f;
        private const float MinSupport = 0.6f;
        private const float SupportTolerance = 0.03f;
        private const float MaxItemSize = 3f;

        internal class Box
        {
            public ItemTeleport.GameItem Item;
            public Transform Transform;
            public Vector3 LocalCenter; // in the item's rotation frame, world units
            public Vector3 LocalSize;
            public float Volume => LocalSize.x * LocalSize.y * LocalSize.z;
        }

        internal class Zone
        {
            public Vector3 Center; // floor level
            public Quaternion Rotation;
            public float Width, Depth;
            public int Nx, Nz;
            public float[,] Heights;

            public Zone(Vector3 center, Quaternion rotation, float width, float depth)
            {
                Center = center;
                Rotation = rotation;
                Width = width;
                Depth = depth;
                Nx = Mathf.Max(1, Mathf.FloorToInt(width / Cell));
                Nz = Mathf.Max(1, Mathf.FloorToInt(depth / Cell));
                Heights = new float[Nx, Nz];
            }

            public Vector3 ToZone(Vector3 world) => Quaternion.Inverse(Rotation) * (world - Center);
            public Vector3 ToWorld(Vector3 local) => Center + Rotation * local;
        }

        public static int Pack(List<ItemTeleport.GameItem> items, Vector3 center, Quaternion rotation, float width, float depth)
        {
            var boxes = new List<Box>();
            foreach (var item in items)
            {
                Transform t = (item.ItemObject as Component)?.transform;
                if (t == null) continue;
                boxes.Add(Measure(item, t));
            }

            // Biggest first: they form the base, small items fill gaps and go on top
            boxes.Sort((a, b) => b.Volume.CompareTo(a.Volume));

            var zone = new Zone(center, rotation, width, depth);
            foreach (var box in boxes) Place(box, zone);

            float maxHeight = 0f;
            foreach (var h in zone.Heights) maxHeight = Mathf.Max(maxHeight, h);
            Hax2.Log1($"Packed {boxes.Count} items, stack height {maxHeight:F2}m");

            new GameObject("PackSupervisor").AddComponent<PackSupervisor>().Init(boxes, zone);
            return boxes.Count;
        }

        internal static void Place(Box box, Zone zone)
        {
            // Lay flat: smallest local axis goes vertical
            Vector3 s = box.LocalSize;
            Vector3 vertical = s.x <= s.y && s.x <= s.z ? Vector3.right : (s.z < s.y ? Vector3.forward : Vector3.up);
            Quaternion flat = Quaternion.FromToRotation(vertical, Vector3.up);

            bool found = false;
            int bestX = 0, bestZ = 0, bestFx = 0, bestFz = 0;
            float bestBase = float.MaxValue;
            Quaternion bestOrientation = flat;
            Vector3 bestDims = Vector3.zero;

            for (int pass = 0; pass < 2 && !found; pass++)
            {
                bool requireSupport = pass == 0;
                foreach (float yaw in new[] { 0f, 90f })
                {
                    Quaternion orientation = Quaternion.Euler(0f, yaw, 0f) * flat;
                    Vector3 dims = Abs(orientation * s);
                    int fx = Mathf.CeilToInt((dims.x + Gap) / Cell);
                    int fz = Mathf.CeilToInt((dims.z + Gap) / Cell);
                    if (fx > zone.Nx || fz > zone.Nz) continue;

                    for (int iz = 0; iz <= zone.Nz - fz; iz++)
                    {
                        for (int ix = 0; ix <= zone.Nx - fx; ix++)
                        {
                            float baseHeight = FootprintMax(zone, ix, iz, fx, fz);
                            if (baseHeight >= bestBase) continue;
                            if (requireSupport && baseHeight > 0.001f && Support(zone, ix, iz, fx, fz, baseHeight) < MinSupport) continue;

                            found = true;
                            bestBase = baseHeight;
                            bestX = ix; bestZ = iz; bestFx = fx; bestFz = fz;
                            bestOrientation = orientation;
                            bestDims = dims;
                        }
                    }
                }
            }

            if (!found)
            {
                // Bigger than the zone: center it on top of everything under it
                bestOrientation = flat;
                bestDims = Abs(flat * s);
                bestFx = Mathf.Min(zone.Nx, Mathf.CeilToInt((bestDims.x + Gap) / Cell));
                bestFz = Mathf.Min(zone.Nz, Mathf.CeilToInt((bestDims.z + Gap) / Cell));
                bestX = (zone.Nx - bestFx) / 2;
                bestZ = (zone.Nz - bestFz) / 2;
                bestBase = FootprintMax(zone, bestX, bestZ, bestFx, bestFz);
            }

            float top = bestBase + bestDims.y + Gap / 2f;
            for (int x = bestX; x < bestX + bestFx; x++)
                for (int z = bestZ; z < bestZ + bestFz; z++)
                    zone.Heights[x, z] = top;

            var local = new Vector3(
                (bestX + bestFx / 2f) * Cell - zone.Nx * Cell / 2f,
                bestBase + bestDims.y / 2f + DropHeight,
                (bestZ + bestFz / 2f) * Cell - zone.Nz * Cell / 2f);

            Quaternion worldRotation = zone.Rotation * bestOrientation;
            Vector3 rootPosition = zone.ToWorld(local) - worldRotation * box.LocalCenter;
            ItemTeleport.MoveItem(box.Item, box.Transform, rootPosition, worldRotation);
        }

        // Rasterize an item already sitting in the zone, using its actual pose
        internal static void Stamp(Box box, Zone zone)
        {
            Vector3 min = Vector3.positiveInfinity, max = Vector3.negativeInfinity;
            foreach (var corner in Corners(box.LocalCenter, box.LocalSize))
            {
                Vector3 p = zone.ToZone(box.Transform.position + box.Transform.rotation * corner);
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }

            int x0 = Mathf.Clamp(Mathf.FloorToInt((min.x + zone.Nx * Cell / 2f) / Cell), 0, zone.Nx - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt((max.x + zone.Nx * Cell / 2f) / Cell), 0, zone.Nx);
            int z0 = Mathf.Clamp(Mathf.FloorToInt((min.z + zone.Nz * Cell / 2f) / Cell), 0, zone.Nz - 1);
            int z1 = Mathf.Clamp(Mathf.CeilToInt((max.z + zone.Nz * Cell / 2f) / Cell), 0, zone.Nz);
            for (int x = x0; x < x1; x++)
                for (int z = z0; z < z1; z++)
                    zone.Heights[x, z] = Mathf.Max(zone.Heights[x, z], max.y + Gap / 2f);
        }

        internal static bool IsOutside(Box box, Zone zone)
        {
            Vector3 c = zone.ToZone(box.Transform.position + box.Transform.rotation * box.LocalCenter);
            return Mathf.Abs(c.x) > zone.Width / 2f + 0.1f || Mathf.Abs(c.z) > zone.Depth / 2f + 0.1f || c.y < -0.5f;
        }

        private static float FootprintMax(Zone zone, int ix, int iz, int fx, int fz)
        {
            float max = 0f;
            for (int x = ix; x < ix + fx; x++)
                for (int z = iz; z < iz + fz; z++)
                    if (zone.Heights[x, z] > max) max = zone.Heights[x, z];
            return max;
        }

        private static float Support(Zone zone, int ix, int iz, int fx, int fz, float baseHeight)
        {
            int supported = 0;
            for (int x = ix; x < ix + fx; x++)
                for (int z = iz; z < iz + fz; z++)
                    if (zone.Heights[x, z] >= baseHeight - SupportTolerance) supported++;
            return (float)supported / (fx * fz);
        }

        private static Box Measure(ItemTeleport.GameItem item, Transform t)
        {
            Rigidbody rb = t.GetComponent<Rigidbody>() ?? t.GetComponentInParent<Rigidbody>();
            Quaternion inv = Quaternion.Inverse(t.rotation);
            Vector3 min = Vector3.positiveInfinity, max = Vector3.negativeInfinity;
            bool found = false;

            void Add(Vector3 world)
            {
                Vector3 p = inv * (world - t.position);
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
                found = true;
            }

            foreach (var col in t.GetComponentsInChildren<Collider>())
            {
                if (!col.enabled || col.isTrigger) continue;
                if (rb != null && col.attachedRigidbody != rb) continue;

                if (TryGetColliderLocalBox(col, out Vector3 c, out Vector3 size))
                {
                    foreach (var corner in Corners(c, size)) Add(col.transform.TransformPoint(corner));
                }
                else
                {
                    Bounds b = col.bounds;
                    foreach (var corner in Corners(b.center, b.size)) Add(corner);
                }
            }

            if (!found)
            {
                foreach (var r in t.GetComponentsInChildren<Renderer>())
                {
                    if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
                    Bounds b = r.bounds;
                    foreach (var corner in Corners(b.center, b.size)) Add(corner);
                }
            }

            Vector3 center = found ? (min + max) / 2f : Vector3.zero;
            Vector3 extent = found ? max - min : Vector3.one * 0.5f;
            if (extent.x > MaxItemSize || extent.y > MaxItemSize || extent.z > MaxItemSize)
            {
                Hax2.Log1($"Item '{t.name}' has suspicious size {extent}, clamping to {MaxItemSize}m");
                extent = Vector3.Min(extent, Vector3.one * MaxItemSize);
            }

            return new Box { Item = item, Transform = t, LocalCenter = center, LocalSize = extent };
        }

        private static bool TryGetColliderLocalBox(Collider col, out Vector3 center, out Vector3 size)
        {
            switch (col)
            {
                case BoxCollider box:
                    center = box.center; size = box.size; return true;
                case SphereCollider sphere:
                    center = sphere.center; size = Vector3.one * sphere.radius * 2f; return true;
                case CapsuleCollider capsule:
                    center = capsule.center;
                    size = Vector3.one * capsule.radius * 2f;
                    size[capsule.direction] = Mathf.Max(capsule.height, capsule.radius * 2f);
                    return true;
                case MeshCollider mesh when mesh.sharedMesh != null:
                    center = mesh.sharedMesh.bounds.center; size = mesh.sharedMesh.bounds.size; return true;
                default:
                    center = size = Vector3.zero; return false;
            }
        }

        private static IEnumerable<Vector3> Corners(Vector3 center, Vector3 size)
        {
            Vector3 e = size / 2f;
            for (int i = 0; i < 8; i++)
                yield return center + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
        }

        private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }

    // Re-places items that rolled or fell out of the zone once physics has settled
    public class PackSupervisor : MonoBehaviour
    {
        private static readonly float[] CheckTimes = { 2f, 4f };

        private List<ItemPacker.Box> boxes;
        private ItemPacker.Zone zone;
        private float startTime;
        private int nextCheck;

        internal void Init(List<ItemPacker.Box> packedBoxes, ItemPacker.Zone packZone)
        {
            boxes = packedBoxes;
            zone = packZone;
            startTime = Time.time;
        }

        private void Update()
        {
            if (boxes == null || nextCheck >= CheckTimes.Length)
            {
                Destroy(gameObject);
                return;
            }
            if (Time.time - startTime < CheckTimes[nextCheck]) return;
            nextCheck++;

            boxes.RemoveAll(b => b.Transform == null);
            var outside = boxes.FindAll(b => ItemPacker.IsOutside(b, zone));
            if (outside.Count == 0)
            {
                Destroy(gameObject);
                return;
            }

            var fresh = new ItemPacker.Zone(zone.Center, zone.Rotation, zone.Width, zone.Depth);
            foreach (var b in boxes)
                if (!outside.Contains(b)) ItemPacker.Stamp(b, fresh);

            outside.Sort((a, b) => b.Volume.CompareTo(a.Volume));
            foreach (var b in outside) ItemPacker.Place(b, fresh);
            zone = fresh;
            Hax2.Log1($"PackSupervisor: re-placed {outside.Count} items that left the zone");
        }
    }
}
