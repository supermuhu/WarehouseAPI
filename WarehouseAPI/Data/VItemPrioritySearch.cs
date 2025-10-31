using System;
using System.Collections.Generic;

namespace WarehouseAPI.Data;

public partial class VItemPrioritySearch
{
    public string ItemId { get; set; } = null!;

    public string QrCode { get; set; } = null!;

    public string ItemName { get; set; } = null!;

    public string CustomerId { get; set; } = null!;

    public string? CustomerName { get; set; }

    public string? PalletId { get; set; }

    public string? PalletBarcode { get; set; }

    public string? ZoneId { get; set; }

    public string? ZoneName { get; set; }

    public string? ShelfId { get; set; }

    public int? ShelfLevel { get; set; }

    public decimal? PositionX { get; set; }

    public decimal? PositionY { get; set; }

    public decimal? PositionZ { get; set; }

    public int? PriorityLevel { get; set; }

    public bool? IsHeavy { get; set; }

    public decimal? Weight { get; set; }

    public DateTime? InboundDate { get; set; }

    public string StoragePosition { get; set; } = null!;
}
