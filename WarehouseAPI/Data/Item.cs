using System;
using System.Collections.Generic;

namespace WarehouseAPI.Data;

public partial class Item
{
    public int ItemId { get; set; }

    public string QrCode { get; set; } = null!;

    public int ProductId { get; set; }

    public int CustomerId { get; set; }

    public string ItemName { get; set; } = null!;

    public string ItemType { get; set; } = null!;

    public decimal Length { get; set; }

    public decimal Width { get; set; }

    public decimal Height { get; set; }

    public decimal? Weight { get; set; }

    public string? Shape { get; set; }

    public int? PriorityLevel { get; set; }

    public bool? IsHeavy { get; set; }

    public bool? IsFragile { get; set; }

    public string? BatchNumber { get; set; }

    public DateOnly? ManufacturingDate { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public decimal? UnitPrice { get; set; }

    public decimal? TotalAmount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Account Customer { get; set; } = null!;

    public virtual ICollection<InboundItem> InboundItems { get; set; } = new List<InboundItem>();

    public virtual ICollection<ItemAllocation> ItemAllocations { get; set; } = new List<ItemAllocation>();

    public virtual ICollection<ItemLocationHistory> ItemLocationHistories { get; set; } = new List<ItemLocationHistory>();

    public virtual ICollection<OutboundItem> OutboundItems { get; set; } = new List<OutboundItem>();
}
