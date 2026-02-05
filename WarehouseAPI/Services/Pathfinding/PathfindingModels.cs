namespace WarehouseAPI.Services.Pathfinding;

/// <summary>
/// Điểm 2D (X, Z) trong không gian kho
/// </summary>
public class Vec2
{
    public decimal X { get; set; }
    public decimal Z { get; set; }

    public Vec2() { }

    public Vec2(decimal x, decimal z)
    {
        X = x;
        Z = z;
    }
}

/// <summary>
/// Tùy chọn cho thuật toán tìm đường
/// </summary>
public class PathfindingOptions
{
    /// <summary>
    /// Kích thước ô lưới (m), mặc định 0.5m
    /// </summary>
    public decimal CellSize { get; set; } = 0.5m;

    /// <summary>
    /// Khoảng cách an toàn với vật cản (m), mặc định 0.25m
    /// </summary>
    public decimal SafetyMargin { get; set; } = 0.25m;

    /// <summary>
    /// Giới hạn số node khám phá, mặc định 200000
    /// </summary>
    public int MaxIterations { get; set; } = 200000;

    /// <summary>
    /// Có tránh các zone không
    /// </summary>
    public bool AvoidZones { get; set; } = false;

    /// <summary>
    /// Zone ID được phép đi qua (nếu AvoidZones = true)
    /// </summary>
    public int? AllowedZoneId { get; set; }

    /// <summary>
    /// Cho phép đi chéo không, mặc định true
    /// </summary>
    public bool AllowDiagonals { get; set; } = true;
}

/// <summary>
/// Kết quả tìm đường
/// </summary>
public class PathResult
{
    /// <summary>
    /// Tìm đường thành công hay không
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Danh sách các điểm trên đường đi
    /// </summary>
    public List<Vec2> Points { get; set; } = new();

    /// <summary>
    /// Tổng khoảng cách (m)
    /// </summary>
    public decimal Distance { get; set; }

    /// <summary>
    /// Số node đã khám phá
    /// </summary>
    public int ExploredNodes { get; set; }

    /// <summary>
    /// Thông báo lỗi nếu có
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Cấu hình lưới
/// </summary>
internal class GridConfig
{
    public int Cols { get; set; }
    public int Rows { get; set; }
    public decimal CellSize { get; set; }
    public decimal OriginX { get; set; }
    public decimal OriginZ { get; set; }
}

/// <summary>
/// Tọa độ lưới
/// </summary>
internal struct GridCoord
{
    public int XIndex { get; set; }
    public int ZIndex { get; set; }
    public int Index { get; set; }
}

/// <summary>
/// Node trong heap
/// </summary>
internal struct HeapNode
{
    public int Index { get; set; }
    public decimal F { get; set; }
}

/// <summary>
/// Bounds của object (AABB)
/// </summary>
internal struct Bounds
{
    public decimal MinX { get; set; }
    public decimal MaxX { get; set; }
    public decimal MinZ { get; set; }
    public decimal MaxZ { get; set; }
}

/// <summary>
/// Request tìm đường tới pallet
/// </summary>
public class FindPathToPalletRequest
{
    public int WarehouseId { get; set; }
    public int PalletId { get; set; }
    public Vec2? StartPosition { get; set; }
    public PathfindingOptions? Options { get; set; }
}

/// <summary>
/// Request tìm đường giữa 2 điểm
/// </summary>
public class FindPathBetweenPointsRequest
{
    public int WarehouseId { get; set; }
    public Vec2 Start { get; set; } = new();
    public Vec2 Goal { get; set; } = new();
    public PathfindingOptions? Options { get; set; }
}

/// <summary>
/// Request tìm đường tới nhiều pallet (outbound picking)
/// </summary>
public class FindPathToMultiplePalletsRequest
{
    public int WarehouseId { get; set; }
    public List<int> PalletIds { get; set; } = new();
    public Vec2? StartPosition { get; set; }
    public PathfindingOptions? Options { get; set; }
}

/// <summary>
/// Kết quả tìm đường tới nhiều pallet
/// </summary>
public class MultiPathResult
{
    public bool Success { get; set; }
    public List<PathResult> Paths { get; set; } = new();
    public decimal TotalDistance { get; set; }
    public int TotalExploredNodes { get; set; }
    public string? ErrorMessage { get; set; }
}
