using System;
using System.Collections.Generic;

namespace WarehouseAPI.Data;

public partial class Warehouse
{
    public int WarehouseId { get; set; }

    public int OwnerId { get; set; }

    public string WarehouseName { get; set; } = null!;

    public decimal Length { get; set; }

    public decimal Width { get; set; }

    public decimal Height { get; set; }

    public string WarehouseType { get; set; } = null!;

    public string? AllowedItemTypes { get; set; }

    public decimal? CheckinPositionX { get; set; }

    public decimal? CheckinPositionY { get; set; }

    public decimal? CheckinPositionZ { get; set; }

    public decimal? CheckinLength { get; set; }

    public decimal? CheckinWidth { get; set; }

    public decimal? CheckinHeight { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<InboundReceipt> InboundReceipts { get; set; } = new List<InboundReceipt>();

    public virtual ICollection<OutboundReceipt> OutboundReceipts { get; set; } = new List<OutboundReceipt>();

    public virtual Account Owner { get; set; } = null!;

    public virtual ICollection<WarehouseGate> WarehouseGates { get; set; } = new List<WarehouseGate>();

    public virtual ICollection<WarehouseZone> WarehouseZones { get; set; } = new List<WarehouseZone>();
}
