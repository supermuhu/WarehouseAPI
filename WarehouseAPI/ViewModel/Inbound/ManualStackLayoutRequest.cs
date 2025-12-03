using System.ComponentModel.DataAnnotations;

namespace WarehouseAPI.ModelView.Inbound;

public class ManualStackUnitRequest
{
    [Required]
    public int UnitIndex { get; set; }

    [Required]
    public decimal LocalX { get; set; }

    [Required]
    public decimal LocalY { get; set; }

    [Required]
    public decimal LocalZ { get; set; }

    [Required]
    public decimal Length { get; set; }

    [Required]
    public decimal Width { get; set; }

    [Required]
    public decimal Height { get; set; }

    [Required]
    public decimal RotationY { get; set; }
}

public class ManualStackLayoutItemRequest
{
    [Required]
    public int InboundItemId { get; set; }

    [Required]
    [MinLength(1)]
    public List<ManualStackUnitRequest> Units { get; set; } = new List<ManualStackUnitRequest>();
}

public class ManualStackLayoutRequest
{
    [Required]
    [MinLength(1)]
    public List<ManualStackLayoutItemRequest> Items { get; set; } = new List<ManualStackLayoutItemRequest>();
}
