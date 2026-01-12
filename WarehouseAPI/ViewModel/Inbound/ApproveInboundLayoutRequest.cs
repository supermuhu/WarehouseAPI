namespace WarehouseAPI.ModelView.Inbound;

public class PreferredPalletLayoutDto
{
    public int PalletId { get; set; }

    /// <summary>
    /// Giá trị ưu tiên do FE gửi (ví dụ vị trí kéo thả trên trục),
    /// số nhỏ = ưu tiên cao hơn. BE dùng để sắp thứ tự xếp pallet.
    /// </summary>
    public decimal? Priority { get; set; }

    /// <summary>
    /// Zone mong muốn cho pallet (nếu FE đã chọn cụ thể). Nếu null, BE tự chọn trong candidate zones.
    /// </summary>
    public int? ZoneId { get; set; }

    /// <summary>
    /// Shelf mong muốn (chỉ áp dụng cho zone rack, pallet box). Nếu null, BE có thể chọn bất kỳ shelf phù hợp.
    /// </summary>
    public int? ShelfId { get; set; }

    /// <summary>
    /// Toạ độ X mong muốn (theo mét, hệ trục toàn cục trong kho). Nếu null, BE tự quét grid.
    /// </summary>
    public decimal? PositionX { get; set; }

    /// <summary>
    /// Toạ độ Z mong muốn (theo mét, hệ trục toàn cục trong kho). Nếu null, BE tự quét grid.
    /// </summary>
    public decimal? PositionZ { get; set; }

    /// <summary>
    /// Góc xoay pallet quanh trục Y (radians). Nếu null, BE hiểu là 0 (không xoay).
    /// </summary>
    public decimal? RotationY { get; set; }

    /// <summary>
    /// Nếu được chỉ định, pallet này sẽ cố gắng được xếp CHỒNG lên pallet có Id tương ứng
    /// (StackLevel = 2) thay vì đặt riêng trên nền/kệ. BE vẫn sẽ kiểm tra thêm các rule an toàn
    /// như pallet dưới cho phép xếp chồng, chiều cao tổng & mặt trên pallet dưới phải phẳng.
    /// </summary>
    public int? StackedOnPalletId { get; set; }
}

public class ApproveInboundLayoutRequest
{
    public List<PreferredPalletLayoutDto>? PreferredLayouts { get; set; }

    /// <summary>
    /// Nếu true, BE sẽ ưu tiên dùng danh sách PreferredLayouts từ FE để sắp thứ tự/xếp pallet,
    /// và chỉ fallback sang thuật toán tự tính khi không đủ thông tin hoặc không xếp được.
    /// Nếu null/false thì hành vi như cũ.
    /// </summary>
    public bool? ForceUsePreferredLayout { get; set; }
}
