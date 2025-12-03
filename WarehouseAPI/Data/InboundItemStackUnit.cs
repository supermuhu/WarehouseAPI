using System;
using System.Collections.Generic;

namespace WarehouseAPI.Data;

public partial class InboundItemStackUnit
{
    public int LayoutId { get; set; }

    public int InboundItemId { get; set; }

    public int UnitIndex { get; set; }

    public decimal LocalX { get; set; }

    public decimal LocalY { get; set; }

    public decimal LocalZ { get; set; }

    public decimal Length { get; set; }

    public decimal Width { get; set; }

    public decimal Height { get; set; }

    public decimal RotationY { get; set; }

    public virtual InboundItem InboundItem { get; set; } = null!;
}
