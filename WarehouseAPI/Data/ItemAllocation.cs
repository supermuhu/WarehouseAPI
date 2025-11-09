using System;
using System.Collections.Generic;

namespace WarehouseAPI.Data;

public partial class ItemAllocation
{
    public int AllocationId { get; set; }

    public int ItemId { get; set; }

    public int PalletId { get; set; }

    public decimal? PositionX { get; set; }

    public decimal? PositionY { get; set; }

    public decimal? PositionZ { get; set; }

    public DateTime? AllocatedAt { get; set; }

    public virtual Item Item { get; set; } = null!;

    public virtual Pallet Pallet { get; set; } = null!;
}
