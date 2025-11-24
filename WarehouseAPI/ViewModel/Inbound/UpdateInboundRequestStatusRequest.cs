using System.ComponentModel.DataAnnotations;

namespace WarehouseAPI.ModelView.Inbound;

public class UpdateInboundRequestStatusRequest
{
    [Required(ErrorMessage = "Status là bắt buộc")]
    [RegularExpression("^(pending|completed|cancelled)$", ErrorMessage = "Status phải là pending, completed hoặc cancelled")]
    public string Status { get; set; } = string.Empty;

    public string? Notes { get; set; }
}

