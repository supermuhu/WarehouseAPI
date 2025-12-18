using WarehouseAPI.ModelView.Common;
using WarehouseAPI.ModelView.Outbound;

namespace WarehouseAPI.Services.Outbound
{
    public interface IOutboundService
    {
        ApiResponse<List<OutboundAvailablePalletViewModel>> GetAvailablePallets(int? accountId, string role, int warehouseId, int? customerId);
        ApiResponse<object> CreateOutboundRequest(int? accountId, string role, CreateOutboundRequestRequest request);
        ApiResponse<List<OutboundRequestListViewModel>> GetOutboundRequests(int? accountId, string role, int? warehouseId);
        ApiResponse<List<OutboundPickingProgressViewModel>> GetOutboundPickingRequests(int? accountId, string role, int? warehouseId);
        ApiResponse<OutboundRequestDetailViewModel> GetOutboundRequestDetail(int receiptId, int? accountId, string role);
        ApiResponse<OutboundReceiptPrintViewModel> GetOutboundReceiptPrintData(int receiptId, int? accountId, string role);
        ApiResponse<object> UpdateOutboundRequestStatus(int receiptId, int? accountId, string role, UpdateOutboundRequestStatusRequest request);
        ApiResponse<List<OutboundPalletPickViewModel>> GetOutboundPalletPicks(int receiptId, int? accountId, string role);
        ApiResponse<object> MarkPalletPicked(int receiptId, int? accountId, string role, OutboundPalletPickRequest request);
    }
}
