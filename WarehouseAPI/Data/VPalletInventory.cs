using System;
using System.Collections.Generic;

namespace WarehouseAPI.Data;

public partial class VPalletInventory
{
    public string PalletId { get; set; } = null!;

    public string Barcode { get; set; } = null!;

    public string? ZoneId { get; set; }

    public string? ZoneName { get; set; }

    public string? CustomerId { get; set; }

    public string? CustomerName { get; set; }

    public string? ShelfId { get; set; }

    public decimal? PositionX { get; set; }

    public decimal? PositionY { get; set; }

    public decimal? PositionZ { get; set; }

    public bool? IsGround { get; set; }

    public int? StackLevel { get; set; }

    public int? ItemCount { get; set; }

    public DateTime? EarliestInboundDate { get; set; }

    public decimal? TotalWeight { get; set; }
}
