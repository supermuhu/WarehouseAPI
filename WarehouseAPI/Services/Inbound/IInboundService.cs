using WarehouseAPI.ModelView.Common;
using WarehouseAPI.ModelView.Inbound;

namespace WarehouseAPI.Services.Inbound
{
    public interface IInboundService
    {
        ApiResponse<object> CreateInboundRequest(int? accountId, string role, CreateInboundRequestRequest request);
        ApiResponse<InboundApprovalViewModel> GetInboundApprovalView(int receiptId, int? accountId, string role);
        ApiResponse<InboundOptimizeLayoutViewModel> OptimizeInboundLayout(int receiptId, int? accountId, string role, ApproveInboundLayoutRequest? request);
        ApiResponse<InboundOptimizeLayoutViewModel> PreviewApproveInboundLayout(int receiptId, int? accountId, string role, ApproveInboundLayoutRequest? request);
        ApiResponse<object> ApproveInboundRequest(int receiptId, int? accountId, string role, ApproveInboundLayoutRequest? request);
        ApiResponse<List<InboundRequestListViewModel>> GetInboundRequests(int? accountId, string role, int? warehouseId, int? zoneId, string? status);
        ApiResponse<InboundRequestDetailViewModel> GetInboundRequestDetail(int receiptId, int? accountId, string role);
        ApiResponse<InboundReceiptPrintViewModel> GetInboundReceiptPrintData(int receiptId, int? accountId, string role);
        ApiResponse<object> UpdateInboundRequestStatus(int receiptId, int? accountId, string role, UpdateInboundRequestStatusRequest request);
        ApiResponse<object> SaveManualStackLayout(int receiptId, int? accountId, string role, ManualStackLayoutRequest request);
    }
}

