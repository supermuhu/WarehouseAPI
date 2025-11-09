using System;
using System.Collections.Generic;

namespace WarehouseAPI.Data;

public partial class VItemPrioritySearch
{
    public int ItemId { get; set; }

    public string QrCode { get; set; } = null!;

    public string ItemName { get; set; } = null!;

    public int CustomerId { get; set; }

    public string? CustomerName { get; set; }

    public int? PalletId { get; set; }

    public string? PalletBarcode { get; set; }

    public int? ZoneId { get; set; }

    public string? ZoneName { get; set; }

    public int? ShelfId { get; set; }

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
