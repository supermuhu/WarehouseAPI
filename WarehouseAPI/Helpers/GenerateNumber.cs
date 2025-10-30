using QRCoder;
using System.Drawing;

namespace WarehouseAPI.Helpers
{
    public class GenerateNumber
    {
        public static string GenerateContractNumber()
        {
            var now = DateTime.Now;
            var year = now.Year;
            var month = now.Month.ToString("D2");
            var day = now.Day.ToString("D2");
            var random = new Random().Next(0, 1000).ToString("D3");
            var contractNumber = $"CR{year}{month}{day}{random}";
            return contractNumber;
        }


    }
}
