namespace WarehouseAPI.ModelView.Inbound;

public class InboundOptimizeLayoutItemViewModel
{
    public int PalletId { get; set; }
    public int ZoneId { get; set; }
    public int? ShelfId { get; set; }
    public decimal PositionX { get; set; }
    public decimal PositionY { get; set; }
    public decimal PositionZ { get; set; }
    public decimal RotationY { get; set; }
    public int StackLevel { get; set; }
    public int? StackedOnPalletId { get; set; }
    public bool IsGround { get; set; }
}

public class InboundOptimizeLayoutViewModel
{
    public int ReceiptId { get; set; }
    public int WarehouseId { get; set; }
    public int CustomerId { get; set; }
    public List<InboundOptimizeLayoutItemViewModel> Layouts { get; set; } = new();
}
