using WarehouseAPI.ModelView.Common;
using WarehouseAPI.ModelView.Pallet;

namespace WarehouseAPI.Services.Pallet
{
    public interface IPalletService
    {
        ApiResponse<List<PalletTemplateViewModel>> GetPalletTemplates();
        ApiResponse<PalletViewModel> CreatePallet(CreatePalletRequest request);
        ApiResponse<PalletViewModel> CreatePalletFromTemplate(int templateId, CreatePalletFromTemplateRequest request);
    }
}

