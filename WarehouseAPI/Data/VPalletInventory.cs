using System;
using System.Collections.Generic;

namespace WarehouseAPI.Data;

public partial class VPalletInventory
{
    public int PalletId { get; set; }

    public string Barcode { get; set; } = null!;

    public int? ZoneId { get; set; }

    public string? ZoneName { get; set; }

    public int? CustomerId { get; set; }

    public string? CustomerName { get; set; }

    public int? ShelfId { get; set; }

    public decimal? PositionX { get; set; }

    public decimal? PositionY { get; set; }

    public decimal? PositionZ { get; set; }

    public bool? IsGround { get; set; }

    public int? StackLevel { get; set; }

    public int? ItemCount { get; set; }

    public DateTime? EarliestInboundDate { get; set; }

    public decimal? TotalWeight { get; set; }
}
