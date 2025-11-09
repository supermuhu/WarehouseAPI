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

    public DateTime? CreatedAt { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<InboundReceipt> InboundReceipts { get; set; } = new List<InboundReceipt>();

    public virtual ICollection<OutboundReceipt> OutboundReceipts { get; set; } = new List<OutboundReceipt>();

    public virtual Account Owner { get; set; } = null!;

    public virtual ICollection<WarehouseZone> WarehouseZones { get; set; } = new List<WarehouseZone>();
}
