using QRCoder;
using System.Drawing;

namespace WarehouseAPI.Helpers
{
    public class GenerateQRCode
    {
        public static string GenerateQR(int idUser, string code)
        {
            QRCodeGenerator qrCodeGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrCodeGenerator.CreateQrCode(code, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(20);

            // Tạo đường dẫn thư mục và tệp
            string folderPath = Path.Combine("wwwroot", "images", "qrcode", idUser.ToString());
            string fileName = $"{code}.jpg";
            string filePath = Path.Combine(folderPath, fileName);

            // Tạo thư mục nếu chưa tồn tại
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Lưu hình ảnh mã QR vào tệp
            qrCodeImage.Save(filePath, System.Drawing.Imaging.ImageFormat.Jpeg);

            // Trả về đường dẫn tới tệp
            return $"/images/qrcode/{idUser.ToString()}/{fileName}";
        }
    }
}
