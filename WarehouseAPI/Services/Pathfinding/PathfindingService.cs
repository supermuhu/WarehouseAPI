using Microsoft.EntityFrameworkCore;
using WarehouseAPI.Data;

namespace WarehouseAPI.Services.Pathfinding;

/// <summary>
/// Service tìm đường đi ngắn nhất trong kho sử dụng thuật toán A*
/// </summary>
public interface IPathfindingService
{
    /// <summary>
    /// Tìm đường từ điểm bắt đầu tới pallet
    /// </summary>
    Task<PathResult> FindPathToPalletAsync(int warehouseId, int palletId, Vec2? startPosition = null, PathfindingOptions? options = null);

    /// <summary>
    /// Tìm đường giữa 2 điểm bất kỳ
    /// </summary>
    Task<PathResult> FindPathBetweenPointsAsync(int warehouseId, Vec2 start, Vec2 goal, PathfindingOptions? options = null);

    /// <summary>
    /// Tìm đường tới nhiều pallet (cho outbound picking)
    /// </summary>
    Task<MultiPathResult> FindPathToMultiplePalletsAsync(int warehouseId, List<int> palletIds, Vec2? startPosition = null, PathfindingOptions? options = null);
}

public class PathfindingService : IPathfindingService
{
    private readonly WarehouseApiContext _context;
    private readonly ILogger<PathfindingService> _logger;

    private static readonly decimal SQRT2 = (decimal)Math.Sqrt(2);

    public PathfindingService(WarehouseApiContext context, ILogger<PathfindingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Public Methods

    public async Task<PathResult> FindPathToPalletAsync(int warehouseId, int palletId, Vec2? startPosition = null, PathfindingOptions? options = null)
    {
        try
        {
            var warehouseData = await GetWarehouseDataAsync(warehouseId);
            if (warehouseData == null)
            {
                return new PathResult { Success = false, ErrorMessage = "Warehouse not found" };
            }

            var pallet = warehouseData.Pallets.FirstOrDefault(p => p.PalletId == palletId);
            if (pallet == null)
            {
                return new PathResult { Success = false, ErrorMessage = "Pallet not found" };
            }

            var start = GetStartPosition(warehouseData, startPosition);
            var effectiveOptions = new PathfindingOptions
            {
                CellSize = options?.CellSize ?? 0.5m,
                SafetyMargin = options?.SafetyMargin ?? 0.25m,
                MaxIterations = options?.MaxIterations ?? 200000,
                AllowDiagonals = false, // Tắt đi chéo cho ground pallet
                AvoidZones = false,
                AllowedZoneId = null
            };

            // Tìm rack chứa pallet (nếu có)
            RackData? rackForPallet = null;
            if (pallet.ShelfId.HasValue)
            {
                foreach (var rack in warehouseData.Racks)
                {
                    if (rack.Shelves.Any(s => s.ShelfId == pallet.ShelfId))
                    {
                        rackForPallet = rack;
                        break;
                    }
                }
            }

            var palletCenterX = pallet.PositionX + pallet.PalletLength / 2;

            if (rackForPallet != null)
            {
                var goal = ComputeRackStandingPoint(start, rackForPallet, palletCenterX, warehouseData.Length, warehouseData.Width, null);
                return FindPathBetweenPointsInternal(warehouseData, start, goal, effectiveOptions);
            }

            // Ground pallet - tìm 4 hướng tiếp cận
            return FindPathToGroundPallet(warehouseData, pallet, start, effectiveOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding path to pallet {PalletId} in warehouse {WarehouseId}", palletId, warehouseId);
            return new PathResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<PathResult> FindPathBetweenPointsAsync(int warehouseId, Vec2 start, Vec2 goal, PathfindingOptions? options = null)
    {
        try
        {
            var warehouseData = await GetWarehouseDataAsync(warehouseId);
            if (warehouseData == null)
            {
                return new PathResult { Success = false, ErrorMessage = "Warehouse not found" };
            }

            var effectiveOptions = options ?? new PathfindingOptions();
            return FindPathBetweenPointsInternal(warehouseData, start, goal, effectiveOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding path in warehouse {WarehouseId}", warehouseId);
            return new PathResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<MultiPathResult> FindPathToMultiplePalletsAsync(int warehouseId, List<int> palletIds, Vec2? startPosition = null, PathfindingOptions? options = null)
    {
        try
        {
            var warehouseData = await GetWarehouseDataAsync(warehouseId);
            if (warehouseData == null)
            {
                return new MultiPathResult { Success = false, ErrorMessage = "Warehouse not found" };
            }

            var result = new MultiPathResult { Success = true };
            var currentPosition = GetStartPosition(warehouseData, startPosition);
            var effectiveOptions = options ?? new PathfindingOptions { AllowDiagonals = false };

            foreach (var palletId in palletIds)
            {
                var pallet = warehouseData.Pallets.FirstOrDefault(p => p.PalletId == palletId);
                if (pallet == null)
                {
                    result.Paths.Add(new PathResult { Success = false, ErrorMessage = $"Pallet {palletId} not found" });
                    continue;
                }

                // Tìm rack chứa pallet (nếu có)
                RackData? rackForPallet = null;
                if (pallet.ShelfId.HasValue)
                {
                    foreach (var rack in warehouseData.Racks)
                    {
                        if (rack.Shelves.Any(s => s.ShelfId == pallet.ShelfId))
                        {
                            rackForPallet = rack;
                            break;
                        }
                    }
                }

                PathResult pathResult;
                var palletCenterX = pallet.PositionX + pallet.PalletLength / 2;

                if (rackForPallet != null)
                {
                    var goal = ComputeRackStandingPoint(currentPosition, rackForPallet, palletCenterX, warehouseData.Length, warehouseData.Width, null);
                    pathResult = FindPathBetweenPointsInternal(warehouseData, currentPosition, goal, effectiveOptions);
                }
                else
                {
                    pathResult = FindPathToGroundPallet(warehouseData, pallet, currentPosition, effectiveOptions);
                }

                result.Paths.Add(pathResult);
                result.TotalExploredNodes += pathResult.ExploredNodes;

                if (pathResult.Success && pathResult.Points.Count > 0)
                {
                    result.TotalDistance += pathResult.Distance;
                    currentPosition = pathResult.Points[^1]; // Cập nhật vị trí hiện tại
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding paths to multiple pallets in warehouse {WarehouseId}", warehouseId);
            return new MultiPathResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    #endregion

    #region Private Methods - A* Algorithm

    private PathResult FindPathBetweenPointsInternal(WarehouseData data, Vec2 start, Vec2 goal, PathfindingOptions options)
    {
        var cellSize = options.CellSize > 0 ? options.CellSize : 0.5m;
        var safetyMargin = options.SafetyMargin >= 0 ? options.SafetyMargin : 0.25m;
        var maxIterations = options.MaxIterations > 0 ? options.MaxIterations : 200000;

        var cols = Math.Max(1, (int)Math.Ceiling((double)(data.Length / cellSize)));
        var rows = Math.Max(1, (int)Math.Ceiling((double)(data.Width / cellSize)));

        var cfg = new GridConfig
        {
            Cols = cols,
            Rows = rows,
            CellSize = cellSize,
            OriginX = 0,
            OriginZ = 0
        };

        var startCell = WorldToGrid(start.X, start.Z, cfg);
        var goalCell = WorldToGrid(goal.X, goal.Z, cfg);

        var startIndex = startCell.Index;
        var goalIndex = goalCell.Index;

        var blocked = BuildObstacleGrid(data, cfg, safetyMargin, startIndex, goalIndex, options);

        var totalCells = cols * rows;
        var gScore = new decimal[totalCells];
        var fScore = new decimal[totalCells];
        var cameFrom = new int[totalCells];
        var closed = new bool[totalCells];

        for (int i = 0; i < totalCells; i++)
        {
            gScore[i] = decimal.MaxValue;
            fScore[i] = decimal.MaxValue;
            cameFrom[i] = -1;
            closed[i] = false;
        }

        var openSet = new MinHeap();

        gScore[startIndex] = 0;
        fScore[startIndex] = HeuristicIndex(startIndex, goalIndex, cfg, cellSize);
        openSet.Push(new HeapNode { Index = startIndex, F = fScore[startIndex] });

        int explored = 0;

        // Neighbors: 4 hướng hoặc 8 hướng
        var neighbors = options.AllowDiagonals
            ? new (int dx, int dz, decimal cost)[]
            {
                (1, 0, 1), (-1, 0, 1), (0, 1, 1), (0, -1, 1),
                (1, 1, SQRT2), (-1, 1, SQRT2), (1, -1, SQRT2), (-1, -1, SQRT2)
            }
            : new (int dx, int dz, decimal cost)[]
            {
                (1, 0, 1), (-1, 0, 1), (0, 1, 1), (0, -1, 1)
            };

        while (openSet.Size > 0 && explored < maxIterations)
        {
            var current = openSet.Pop();
            if (current == null) break;

            var currentIndex = current.Value.Index;

            if (closed[currentIndex]) continue;

            if (currentIndex == goalIndex)
            {
                var rawPath = ReconstructPath(cameFrom, currentIndex, cfg);
                var simplified = CompressPath(rawPath);
                var distance = ComputeDistance(simplified);
                return new PathResult
                {
                    Success = true,
                    Points = simplified,
                    Distance = distance,
                    ExploredNodes = explored
                };
            }

            closed[currentIndex] = true;
            explored++;

            var (xIndex, zIndex) = IndexToCoord(currentIndex, cfg);

            foreach (var (dx, dz, cost) in neighbors)
            {
                var nx = xIndex + dx;
                var nz = zIndex + dz;

                if (nx < 0 || nx >= cols || nz < 0 || nz >= rows) continue;

                var nIndex = nz * cols + nx;
                if (blocked[nIndex]) continue;
                if (closed[nIndex]) continue;

                // Turn penalty để tránh đường zic-zac
                decimal turnPenalty = 0;
                var parentIndex = cameFrom[currentIndex];
                if (parentIndex != -1)
                {
                    var (parentX, parentZ) = IndexToCoord(parentIndex, cfg);
                    var prevDx = Math.Sign(xIndex - parentX);
                    var prevDz = Math.Sign(zIndex - parentZ);
                    if ((prevDx != 0 || prevDz != 0) && (dx != prevDx || dz != prevDz))
                    {
                        turnPenalty = cellSize * 1.5m;
                    }
                }

                var tentativeG = gScore[currentIndex] + cost * cellSize + turnPenalty;
                if (tentativeG >= gScore[nIndex]) continue;

                cameFrom[nIndex] = currentIndex;
                gScore[nIndex] = tentativeG;
                fScore[nIndex] = tentativeG + HeuristicIndex(nIndex, goalIndex, cfg, cellSize);
                openSet.Push(new HeapNode { Index = nIndex, F = fScore[nIndex] });
            }
        }

        _logger.LogWarning("Pathfinding failed: start=({StartX},{StartZ}), goal=({GoalX},{GoalZ}), explored={Explored}",
            start.X, start.Z, goal.X, goal.Z, explored);

        return new PathResult
        {
            Success = false,
            Points = new List<Vec2>(),
            Distance = 0,
            ExploredNodes = explored,
            ErrorMessage = "No path found"
        };
    }

    private PathResult FindPathToGroundPallet(WarehouseData data, PalletData pallet, Vec2 start, PathfindingOptions options)
    {
        var rectMinX = pallet.PositionX;
        var rectMaxX = pallet.PositionX + pallet.PalletLength;
        var rectMinZ = pallet.PositionZ;
        var rectMaxZ = pallet.PositionZ + pallet.PalletWidth;

        var standOffset = 0.9m;
        var sideMargin = 0.4m;

        var candidates = new List<Vec2>
        {
            // Left side
            new Vec2(
                Clamp(rectMinX - standOffset, 0.1m, data.Length - 0.1m),
                Clamp(start.Z, rectMinZ + sideMargin, rectMaxZ - sideMargin)
            ),
            // Right side
            new Vec2(
                Clamp(rectMaxX + standOffset, 0.1m, data.Length - 0.1m),
                Clamp(start.Z, rectMinZ + sideMargin, rectMaxZ - sideMargin)
            ),
            // Front side (smaller Z)
            new Vec2(
                Clamp(start.X, rectMinX + sideMargin, rectMaxX - sideMargin),
                Clamp(rectMinZ - standOffset, 0.1m, data.Width - 0.1m)
            ),
            // Back side (larger Z)
            new Vec2(
                Clamp(start.X, rectMinX + sideMargin, rectMaxX - sideMargin),
                Clamp(rectMaxZ + standOffset, 0.1m, data.Width - 0.1m)
            )
        };

        // Remove duplicates
        var uniqueCandidates = candidates
            .GroupBy(c => (Math.Round((double)c.X, 3), Math.Round((double)c.Z, 3)))
            .Select(g => g.First())
            .ToList();

        PathResult? best = null;
        int exploredTotal = 0;

        foreach (var goal in uniqueCandidates)
        {
            var result = FindPathBetweenPointsInternal(data, start, goal, options);
            exploredTotal += result.ExploredNodes;

            if (!result.Success || result.Points.Count < 2) continue;

            if (best == null || result.Distance < best.Distance)
            {
                best = result;
            }
        }

        if (best == null)
        {
            return new PathResult
            {
                Success = false,
                Points = new List<Vec2>(),
                Distance = 0,
                ExploredNodes = exploredTotal,
                ErrorMessage = "No path found to pallet"
            };
        }

        return new PathResult
        {
            Success = true,
            Points = best.Points,
            Distance = best.Distance,
            ExploredNodes = exploredTotal
        };
    }

    #endregion

    #region Helper Methods

    private async Task<WarehouseData?> GetWarehouseDataAsync(int warehouseId)
    {
        var warehouse = await _context.Warehouses.FindAsync(warehouseId);
        if (warehouse == null) return null;

        var zones = await _context.WarehouseZones
            .Where(z => z.WarehouseId == warehouseId)
            .ToListAsync();

        var racks = await _context.Racks
            .Include(r => r.Shelves)
            .Where(r => zones.Select(z => z.ZoneId).Contains(r.ZoneId))
            .ToListAsync();

        var pallets = await _context.PalletLocations
            .Include(p => p.Zone)
            .Include(p => p.Pallet)
            .Where(p => p.Zone != null && p.Zone.WarehouseId == warehouseId)
            .ToListAsync();

        return new WarehouseData
        {
            WarehouseId = warehouseId,
            Length = warehouse.Length,
            Width = warehouse.Width,
            Height = warehouse.Height,
            CheckinPositionX = warehouse.CheckinPositionX,
            CheckinPositionZ = warehouse.CheckinPositionZ,
            CheckinLength = warehouse.CheckinLength,
            CheckinWidth = warehouse.CheckinWidth,
            Zones = zones.Select(z => new ZoneData
            {
                ZoneId = z.ZoneId,
                PositionX = z.PositionX,
                PositionZ = z.PositionZ,
                Length = z.Length,
                Width = z.Width
            }).ToList(),
            Racks = racks.Select(r => new RackData
            {
                RackId = r.RackId,
                PositionX = r.PositionX,
                PositionZ = r.PositionZ,
                Length = r.Length,
                Width = r.Width,
                RotationY = r.RotationY,
                Shelves = r.Shelves.Select(s => new ShelfData { ShelfId = s.ShelfId }).ToList()
            }).ToList(),
            Pallets = pallets.Select(p => new PalletData
            {
                PalletId = p.PalletId,
                PositionX = p.PositionX,
                PositionZ = p.PositionZ,
                PalletLength = p.Pallet.Length,
                PalletWidth = p.Pallet.Width,
                RotationY = p.RotationY,
                ShelfId = p.ShelfId,
                ZoneId = p.ZoneId
            }).ToList()
        };
    }

    private Vec2 GetStartPosition(WarehouseData data, Vec2? startPosition)
    {
        if (startPosition != null)
        {
            return startPosition;
        }

        if (data.CheckinPositionX.HasValue && data.CheckinPositionZ.HasValue)
        {
            var x = data.CheckinPositionX.Value;
            var z = data.CheckinPositionZ.Value;

            if (data.CheckinLength.HasValue && data.CheckinLength > 0 &&
                data.CheckinWidth.HasValue && data.CheckinWidth > 0)
            {
                x += data.CheckinLength.Value / 2;
                z += data.CheckinWidth.Value / 2;
            }

            return new Vec2(x, z);
        }

        return new Vec2(0, 0);
    }

    private static Bounds GetRotatedBounds(decimal posX, decimal posZ, decimal length, decimal width, decimal? rotationY)
    {
        var angle = rotationY ?? 0;

        if (angle == 0)
        {
            return new Bounds
            {
                MinX = posX,
                MaxX = posX + length,
                MinZ = posZ,
                MaxZ = posZ + width
            };
        }

        var centerX = posX + length / 2;
        var centerZ = posZ + width / 2;
        var halfL = length / 2;
        var halfW = width / 2;
        var cos = (decimal)Math.Cos((double)angle);
        var sin = (decimal)Math.Sin((double)angle);

        var corners = new (decimal dx, decimal dz)[]
        {
            (-halfL, -halfW), (halfL, -halfW), (halfL, halfW), (-halfL, halfW)
        };

        decimal minX = decimal.MaxValue, maxX = decimal.MinValue;
        decimal minZ = decimal.MaxValue, maxZ = decimal.MinValue;

        foreach (var (dx, dz) in corners)
        {
            var x = centerX + dx * cos - dz * sin;
            var z = centerZ + dx * sin + dz * cos;
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (z < minZ) minZ = z;
            if (z > maxZ) maxZ = z;
        }

        return new Bounds { MinX = minX, MaxX = maxX, MinZ = minZ, MaxZ = maxZ };
    }

    private static decimal Clamp(decimal value, decimal min, decimal max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private static GridCoord WorldToGrid(decimal x, decimal z, GridConfig cfg)
    {
        var relX = x - cfg.OriginX;
        var relZ = z - cfg.OriginZ;

        var xIndex = (int)Math.Floor((double)(relX / cfg.CellSize));
        var zIndex = (int)Math.Floor((double)(relZ / cfg.CellSize));

        xIndex = Math.Clamp(xIndex, 0, cfg.Cols - 1);
        zIndex = Math.Clamp(zIndex, 0, cfg.Rows - 1);

        return new GridCoord
        {
            XIndex = xIndex,
            ZIndex = zIndex,
            Index = zIndex * cfg.Cols + xIndex
        };
    }

    private static Vec2 GridToWorld(int xIndex, int zIndex, GridConfig cfg)
    {
        var x = cfg.OriginX + (xIndex + 0.5m) * cfg.CellSize;
        var z = cfg.OriginZ + (zIndex + 0.5m) * cfg.CellSize;
        return new Vec2(x, z);
    }

    private static (int xIndex, int zIndex) IndexToCoord(int index, GridConfig cfg)
    {
        var zIndex = index / cfg.Cols;
        var xIndex = index - zIndex * cfg.Cols;
        return (xIndex, zIndex);
    }

    private static decimal HeuristicIndex(int aIndex, int bIndex, GridConfig cfg, decimal cellSize)
    {
        var (ax, az) = IndexToCoord(aIndex, cfg);
        var (bx, bz) = IndexToCoord(bIndex, cfg);
        var dx = Math.Abs(ax - bx);
        var dz = Math.Abs(az - bz);
        var minD = Math.Min(dx, dz);
        var maxD = Math.Max(dx, dz);
        return (SQRT2 * minD + (maxD - minD)) * cellSize;
    }

    private bool[] BuildObstacleGrid(WarehouseData data, GridConfig cfg, decimal safetyMargin, int startIndex, int goalIndex, PathfindingOptions options)
    {
        var blocked = new bool[cfg.Cols * cfg.Rows];
        var margin = Math.Max(0, safetyMargin);

        void MarkRectangle(decimal minX, decimal maxX, decimal minZ, decimal maxZ)
        {
            var fromX = Math.Clamp((int)Math.Floor((double)((minX - cfg.OriginX) / cfg.CellSize)), 0, cfg.Cols - 1);
            var toX = Math.Clamp((int)Math.Ceiling((double)((maxX - cfg.OriginX) / cfg.CellSize)) - 1, 0, cfg.Cols - 1);
            var fromZ = Math.Clamp((int)Math.Floor((double)((minZ - cfg.OriginZ) / cfg.CellSize)), 0, cfg.Rows - 1);
            var toZ = Math.Clamp((int)Math.Ceiling((double)((maxZ - cfg.OriginZ) / cfg.CellSize)) - 1, 0, cfg.Rows - 1);

            for (int zIdx = fromZ; zIdx <= toZ; zIdx++)
            {
                var rowOffset = zIdx * cfg.Cols;
                for (int xIdx = fromX; xIdx <= toX; xIdx++)
                {
                    blocked[rowOffset + xIdx] = true;
                }
            }
        }

        // Đánh dấu zones là vật cản (nếu cần)
        if (options.AvoidZones)
        {
            var zoneMargin = margin > 0 ? margin : 0;
            foreach (var zone in data.Zones)
            {
                if (options.AllowedZoneId.HasValue && zone.ZoneId == options.AllowedZoneId.Value)
                    continue;

                MarkRectangle(
                    zone.PositionX - zoneMargin,
                    zone.PositionX + zone.Length + zoneMargin,
                    zone.PositionZ - zoneMargin,
                    zone.PositionZ + zone.Width + zoneMargin
                );
            }
        }

        // Đánh dấu racks là vật cản
        if (margin > 0)
        {
            foreach (var rack in data.Racks)
            {
                var bounds = GetRotatedBounds(rack.PositionX, rack.PositionZ, rack.Length, rack.Width, rack.RotationY);
                MarkRectangle(
                    bounds.MinX - margin,
                    bounds.MaxX + margin,
                    bounds.MinZ - margin,
                    bounds.MaxZ + margin
                );
            }

            // Đánh dấu pallets là vật cản
            foreach (var pallet in data.Pallets)
            {
                var bounds = GetRotatedBounds(pallet.PositionX, pallet.PositionZ, pallet.PalletLength, pallet.PalletWidth, pallet.RotationY);
                MarkRectangle(
                    bounds.MinX - margin,
                    bounds.MaxX + margin,
                    bounds.MinZ - margin,
                    bounds.MaxZ + margin
                );
            }
        }

        // Đảm bảo start và goal không bị block
        blocked[startIndex] = false;
        blocked[goalIndex] = false;

        // Clear neighborhood của start và goal
        void ClearNeighborhood(int centerIndex, int radius)
        {
            if (radius <= 0) return;
            var (xIdx, zIdx) = IndexToCoord(centerIndex, cfg);
            for (int dz = -radius; dz <= radius; dz++)
            {
                var nz = zIdx + dz;
                if (nz < 0 || nz >= cfg.Rows) continue;
                var rowOffset = nz * cfg.Cols;
                for (int dx = -radius; dx <= radius; dx++)
                {
                    var nx = xIdx + dx;
                    if (nx < 0 || nx >= cfg.Cols) continue;
                    blocked[rowOffset + nx] = false;
                }
            }
        }

        ClearNeighborhood(startIndex, 1);
        ClearNeighborhood(goalIndex, 1);

        return blocked;
    }

    private static List<Vec2> ReconstructPath(int[] cameFrom, int currentIndex, GridConfig cfg)
    {
        var path = new List<Vec2>();
        var idx = currentIndex;
        while (idx != -1)
        {
            var (xIdx, zIdx) = IndexToCoord(idx, cfg);
            path.Add(GridToWorld(xIdx, zIdx, cfg));
            idx = cameFrom[idx];
        }
        path.Reverse();
        return path;
    }

    private static List<Vec2> CompressPath(List<Vec2> points)
    {
        if (points.Count <= 2) return points;

        var result = new List<Vec2> { points[0] };

        var prevDx = Math.Sign((double)(points[1].X - points[0].X));
        var prevDz = Math.Sign((double)(points[1].Z - points[0].Z));

        for (int i = 1; i < points.Count - 1; i++)
        {
            var dx = Math.Sign((double)(points[i + 1].X - points[i].X));
            var dz = Math.Sign((double)(points[i + 1].Z - points[i].Z));
            if (dx != prevDx || dz != prevDz)
            {
                result.Add(points[i]);
                prevDx = dx;
                prevDz = dz;
            }
        }

        result.Add(points[^1]);
        return result;
    }

    private static decimal ComputeDistance(List<Vec2> points)
    {
        if (points.Count < 2) return 0;

        decimal dist = 0;
        for (int i = 1; i < points.Count; i++)
        {
            var dx = (double)(points[i].X - points[i - 1].X);
            var dz = (double)(points[i].Z - points[i - 1].Z);
            dist += (decimal)Math.Sqrt(dx * dx + dz * dz);
        }
        return dist;
    }

    private static Vec2 ComputeRackStandingPoint(Vec2 source, RackData rack, decimal palletCenterX, decimal warehouseLength, decimal warehouseWidth, ZoneData? zone)
    {
        var minX = rack.PositionX;
        var maxX = rack.PositionX + rack.Length;
        var minZ = rack.PositionZ;
        var maxZ = rack.PositionZ + rack.Width;

        var standOffset = 0.9m;
        var sideMargin = 0.4m;

        decimal x, z;

        // Đứng trước/sau rack theo trục Z
        if (source.Z <= (minZ + maxZ) / 2)
        {
            z = minZ - standOffset;
        }
        else
        {
            z = maxZ + standOffset;
        }

        x = Clamp(palletCenterX, minX + sideMargin, maxX - sideMargin);

        if (zone != null)
        {
            x = Clamp(x, zone.PositionX + 0.1m, zone.PositionX + zone.Length - 0.1m);
            z = Clamp(z, zone.PositionZ + 0.1m, zone.PositionZ + zone.Width - 0.1m);
        }
        else
        {
            x = Clamp(x, 0.1m, warehouseLength - 0.1m);
            z = Clamp(z, 0.1m, warehouseWidth - 0.1m);
        }

        return new Vec2(x, z);
    }

    #endregion
}

#region Internal Data Models

internal class WarehouseData
{
    public int WarehouseId { get; set; }
    public decimal Length { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public decimal? CheckinPositionX { get; set; }
    public decimal? CheckinPositionZ { get; set; }
    public decimal? CheckinLength { get; set; }
    public decimal? CheckinWidth { get; set; }
    public List<ZoneData> Zones { get; set; } = new();
    public List<RackData> Racks { get; set; } = new();
    public List<PalletData> Pallets { get; set; } = new();
}

internal class ZoneData
{
    public int ZoneId { get; set; }
    public decimal PositionX { get; set; }
    public decimal PositionZ { get; set; }
    public decimal Length { get; set; }
    public decimal Width { get; set; }
}

internal class RackData
{
    public int RackId { get; set; }
    public decimal PositionX { get; set; }
    public decimal PositionZ { get; set; }
    public decimal Length { get; set; }
    public decimal Width { get; set; }
    public decimal? RotationY { get; set; }
    public List<ShelfData> Shelves { get; set; } = new();
}

internal class ShelfData
{
    public int ShelfId { get; set; }
}

internal class PalletData
{
    public int PalletId { get; set; }
    public decimal PositionX { get; set; }
    public decimal PositionZ { get; set; }
    public decimal PalletLength { get; set; }
    public decimal PalletWidth { get; set; }
    public decimal? RotationY { get; set; }
    public int? ShelfId { get; set; }
    public int? ZoneId { get; set; }
}

#endregion
