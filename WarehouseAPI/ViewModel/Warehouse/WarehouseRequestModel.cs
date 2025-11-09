using System.ComponentModel.DataAnnotations;

namespace WarehouseAPI.ViewModel.Warehouse
{
    /// <summary>
    /// Model để tạo kho mới
    /// </summary>
    public class CreateWarehouseModel
    {
        [Required(ErrorMessage = "Tên kho là bắt buộc")]
        public string WarehouseName { get; set; } = string.Empty;

        [Required(ErrorMessage = "ID chủ kho là bắt buộc")]
        public int OwnerId { get; set; }

        [Required(ErrorMessage = "Chiều dài là bắt buộc")]
        [Range(1, 1000, ErrorMessage = "Chiều dài phải từ 1 đến 1000 mét")]
        public decimal Length { get; set; }

        [Required(ErrorMessage = "Chiều rộng là bắt buộc")]
        [Range(1, 1000, ErrorMessage = "Chiều rộng phải từ 1 đến 1000 mét")]
        public decimal Width { get; set; }

        [Required(ErrorMessage = "Chiều cao là bắt buộc")]
        [Range(1, 50, ErrorMessage = "Chiều cao phải từ 1 đến 50 mét")]
        public decimal Height { get; set; }

        [Required(ErrorMessage = "Loại kho là bắt buộc")]
        [RegularExpression("^(small|medium|large)$", ErrorMessage = "Loại kho phải là small, medium hoặc large")]
        public string WarehouseType { get; set; } = string.Empty;

        public string? AllowedItemTypes { get; set; } // JSON string: ["bag", "box", "pallet"]
    }

    /// <summary>
    /// Model để cập nhật thông tin kho
    /// </summary>
    public class UpdateWarehouseModel
    {
        public string? WarehouseName { get; set; }
        
        [Range(1, 1000, ErrorMessage = "Chiều dài phải từ 1 đến 1000 mét")]
        public decimal? Length { get; set; }

        [Range(1, 1000, ErrorMessage = "Chiều rộng phải từ 1 đến 1000 mét")]
        public decimal? Width { get; set; }

        [Range(1, 50, ErrorMessage = "Chiều cao phải từ 1 đến 50 mét")]
        public decimal? Height { get; set; }

        [RegularExpression("^(small|medium|large)$", ErrorMessage = "Loại kho phải là small, medium hoặc large")]
        public string? WarehouseType { get; set; }

        public string? AllowedItemTypes { get; set; }

        [RegularExpression("^(active|inactive)$", ErrorMessage = "Trạng thái phải là active hoặc inactive")]
        public string? Status { get; set; }
    }

    /// <summary>
    /// Model để tìm kiếm/lọc kho
    /// </summary>
    public class WarehouseFilterModel
    {
        public int? OwnerId { get; set; }
        public string? WarehouseType { get; set; }
        public string? Status { get; set; }
        public string? SearchKeyword { get; set; }
    }
}
