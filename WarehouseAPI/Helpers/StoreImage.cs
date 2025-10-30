using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using TicketWebAPI.Data;
using TicketWebAPI.Helpers;

namespace TicketWebAPI.Helpers
{
    public static class StoreImage
    {
        public static string SaveImage(IFormFile imageFile, string directory)
        {
            try
            {
                if (imageFile == null || imageFile.Length == 0)
                {
                    return null;
                }

                var allowedMimeTypes = new[]
                {
                        "image/jpeg",
                        "image/png",
                        //"image/gif",
                        //"image/bmp",
                        "image/webp",
                        "image/tiff",
                        //"image/svg+xml",
                        "image/x-icon",
                        "image/jfif"
                    };
                if (!allowedMimeTypes.Contains(imageFile.ContentType))
                {
                    return null;
                }
                var fileName = GenerateUniqueFileName(imageFile.FileName);
                var filePath = Path.Combine(directory, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    imageFile.CopyTo(stream);
                }

                return fileName;
            }
            catch
            {
                return null; // Xử lý lỗi nếu cần
            }
        }

        private static string GenerateUniqueFileName(string originalFileName)
        {
            var uniqueFileName = $"{Guid.NewGuid()}.jpg";
            return uniqueFileName;
        }

        public static bool DeleteImage(string directory)
        {
            try
            {
                if (string.IsNullOrEmpty(directory))
                {
                    return false; // Đường dẫn hoặc tên tệp không hợp lệ
                }

                if (File.Exists(directory))
                {
                    File.Delete(directory); // Xóa tệp
                    return true; // Xóa thành công
                }

                return false; // Tệp không tồn tại
            }
            catch (Exception)
            {
                return false; // Xảy ra lỗi khi xóa tệp
            }
        }
    }
}
//string imagePath = $"/images/AvatarUser/512_default.png";
//string imageName = "512_default.png";

// Đường dẫn thư mục lưu trữ hình ảnh
//var avatarDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "AvatarUser");
//Directory.CreateDirectory(avatarDirectory);

//if (model.ImageFile != null)
//{
//    // Đường dẫn thư mục lưu trữ hình ảnh
//    var avatarDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "AvatarUser");
//    Directory.CreateDirectory(avatarDirectory);
//    var fileName = StoreImage.SaveImage(model.ImageFile, avatarDirectory);
//    if (fileName != null)
//    {
//        userProfile.ImagePath = $"/images/avatarUser/{fileName}";
//        userProfile.ImageName = fileName;
//    }
//}
//}