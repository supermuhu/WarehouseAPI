using System;

namespace WarehouseAPI.Data;

/// <summary>
/// Cấu hình layout cho zone ground với block (vùng để pallet) và aisle (đường đi)
/// </summary>
public partial class ZoneLayoutConfig
{
    public int ConfigId { get; set; }

    public int ZoneId { get; set; }

    /// <summary>
    /// Chiều rộng của block (vùng để pallet) theo trục X (m)
    /// VD: 6m cho block 6mx6m, hoặc 1000m nếu muốn liên tục
    /// </summary>
    public decimal BlockWidth { get; set; }

    /// <summary>
    /// Chiều sâu của block (vùng để pallet) theo trục X (m)
    /// VD: 3m cho block 3m, hoặc 18m cho block dài 18m
    /// </summary>
    public decimal BlockDepth { get; set; }

    /// <summary>
    /// Chiều rộng của BLOCK ĐẦU TIÊN theo trục Z (m) - nếu khác kích thước các block sau
    /// Nếu null thì block đầu tiên có cùng kích thước với các block khác (BlockWidth)
    /// VD: 1.5m cho hàng đầu tiên, trong khi các hàng sau là 3m
    /// </summary>
    public decimal? FirstBlockWidth { get; set; }

    /// <summary>
    /// Chiều sâu của BLOCK ĐẦU TIÊN theo trục X (m) - nếu khác kích thước các block sau
    /// Nếu null thì block đầu tiên có cùng kích thước với các block khác (BlockDepth)
    /// VD: 1.5m cho cột đầu tiên, trong khi các cột sau là 18m
    /// </summary>
    public decimal? FirstBlockDepth { get; set; }

    /// <summary>
    /// Chiều rộng đường đi NGANG (theo trục X, giữa các block theo chiều Z) (m)
    /// </summary>
    public decimal HorizontalAisleWidth { get; set; }

    /// <summary>
    /// Chiều rộng đường đi DỌC (theo trục Z, giữa các block theo chiều X) (m)
    /// </summary>
    public decimal VerticalAisleWidth { get; set; }

    /// <summary>
    /// Offset bắt đầu từ góc zone theo trục X (m)
    /// </summary>
    public decimal StartOffsetX { get; set; }

    /// <summary>
    /// Offset bắt đầu từ góc zone theo trục Z (m)
    /// </summary>
    public decimal StartOffsetZ { get; set; }

    /// <summary>
    /// Có active không
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    public virtual WarehouseZone Zone { get; set; } = null!;
}
