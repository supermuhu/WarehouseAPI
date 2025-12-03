using System;
using System.Collections.Generic;

namespace WarehouseAPI.Data;

public partial class WarehouseGate
{
    public int GateId { get; set; }

    public int WarehouseId { get; set; }

    public string? GateName { get; set; }

    public decimal PositionX { get; set; }

    public decimal PositionY { get; set; }

    public decimal PositionZ { get; set; }

    public decimal? Length { get; set; }

    public decimal? Width { get; set; }

    public decimal? Height { get; set; }

    public string GateType { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual Warehouse Warehouse { get; set; } = null!;
}
