using System;
using System.Collections.Generic;

namespace WarehouseAPI.Data;

public partial class Pallet
{
    public int PalletId { get; set; }

    public string Barcode { get; set; } = null!;

    public decimal Length { get; set; }

    public decimal Width { get; set; }

    public decimal Height { get; set; }

    public decimal MaxWeight { get; set; }

    public decimal MaxStackHeight { get; set; }

    public string? PalletType { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<InboundItem> InboundItems { get; set; } = new List<InboundItem>();

    public virtual ICollection<ItemAllocation> ItemAllocations { get; set; } = new List<ItemAllocation>();

    public virtual ICollection<PalletLocation> PalletLocationPallets { get; set; } = new List<PalletLocation>();

    public virtual ICollection<PalletLocation> PalletLocationStackedOnPalletNavigations { get; set; } = new List<PalletLocation>();

    public virtual ICollection<ItemLocationHistory> ItemLocationHistories { get; set; } = new List<ItemLocationHistory>();

    public virtual ICollection<OutboundPalletPick> OutboundPalletPicks { get; set; } = new List<OutboundPalletPick>();
}
