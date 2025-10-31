using System;
using System.Collections.Generic;

namespace WarehouseAPI.Data;

public partial class PalletLocation
{
    public string LocationId { get; set; } = null!;

    public string PalletId { get; set; } = null!;

    public string ZoneId { get; set; } = null!;

    public string? ShelfId { get; set; }

    public decimal PositionX { get; set; }

    public decimal PositionY { get; set; }

    public decimal PositionZ { get; set; }

    public int? StackLevel { get; set; }

    public string? StackedOnPallet { get; set; }

    public bool? IsGround { get; set; }

    public DateTime? AssignedAt { get; set; }

    public virtual ICollection<ItemLocationHistory> ItemLocationHistories { get; set; } = new List<ItemLocationHistory>();

    public virtual Pallet Pallet { get; set; } = null!;

    public virtual Shelf? Shelf { get; set; }

    public virtual Pallet? StackedOnPalletNavigation { get; set; }

    public virtual WarehouseZone Zone { get; set; } = null!;
}
