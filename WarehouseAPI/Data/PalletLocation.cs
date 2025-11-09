using System;
using System.Collections.Generic;

namespace WarehouseAPI.Data;

public partial class PalletLocation
{
    public int LocationId { get; set; }

    public int PalletId { get; set; }

    public int ZoneId { get; set; }

    public int? ShelfId { get; set; }

    public decimal PositionX { get; set; }

    public decimal PositionY { get; set; }

    public decimal PositionZ { get; set; }

    public int? StackLevel { get; set; }

    public int? StackedOnPallet { get; set; }

    public bool? IsGround { get; set; }

    public DateTime? AssignedAt { get; set; }

    public virtual ICollection<ItemLocationHistory> ItemLocationHistories { get; set; } = new List<ItemLocationHistory>();

    public virtual Pallet Pallet { get; set; } = null!;

    public virtual Shelf? Shelf { get; set; }

    public virtual Pallet? StackedOnPalletNavigation { get; set; }

    public virtual WarehouseZone Zone { get; set; } = null!;
}
