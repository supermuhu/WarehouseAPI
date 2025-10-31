// using Microsoft.EntityFrameworkCore;
// using WarehouseAPI.Data;
// using WarehouseAPI.Helpers;
// using WarehouseAPI.ModelView.UserStatus;

// namespace WarehouseAPI.Services.UserStatus
// {
//     public class UserStatusService : IUserStatusService
//     {
//         private readonly WarehouseAPIContext db;

//         public UserStatusService(WarehouseAPIContext db)
//         {
//             this.db = db;
//         }

//         public ApiResponse GetAll()
//         {
//             try
//             {
//                 var statuses = db.UserStatuses
//                     .OrderBy(s => s.IdUserStatus)
//                     .ToList();

//                 return new ApiResponse(200, "Lấy danh sách user statuses thành công", statuses);
//             }
//             catch (Exception e)
//             {
//                 return new ApiResponse(502, e.Message, null);
//             }
//         }

//         public ApiResponse GetById(int id)
//         {
//             try
//             {
//                 var status = db.UserStatuses.FirstOrDefault(s => s.IdUserStatus == id);

//                 if (status == null)
//                 {
//                     return new ApiResponse(404, "Không tìm thấy user status", null);
//                 }

//                 return new ApiResponse(200, "Lấy thông tin user status thành công", status);
//             }
//             catch (Exception e)
//             {
//                 return new ApiResponse(502, e.Message, null);
//             }
//         }

//         public ApiResponse Create(UserStatusCreateModel model)
//         {
//             try
//             {
//                 // Check if status name already exists
//                 var existingStatus = db.UserStatuses.FirstOrDefault(s => s.StatusName.ToLower() == model.StatusName.ToLower());
//                 if (existingStatus != null)
//                 {
//                     return new ApiResponse(400, "Tên status đã tồn tại", null);
//                 }

//                 var status = new Data.UserStatus
//                 {
//                     StatusName = model.StatusName,
//                     Description = model.Description
//                 };

//                 db.UserStatuses.Add(status);
//                 db.SaveChanges();

//                 return new ApiResponse(201, "Tạo user status thành công", status.IdUserStatus);
//             }
//             catch (Exception e)
//             {
//                 return new ApiResponse(502, e.Message, null);
//             }
//         }

//         public ApiResponse Update(int id, UserStatusUpdateModel model)
//         {
//             try
//             {
//                 var status = db.UserStatuses.FirstOrDefault(s => s.IdUserStatus == id);
//                 if (status == null)
//                 {
//                     return new ApiResponse(404, "Không tìm thấy user status", null);
//                 }

//                 if (!string.IsNullOrEmpty(model.StatusName))
//                 {
//                     // Check if new status name already exists (excluding current status)
//                     var existingStatus = db.UserStatuses.FirstOrDefault(s => 
//                         s.StatusName.ToLower() == model.StatusName.ToLower() && s.IdUserStatus != id);
//                     if (existingStatus != null)
//                     {
//                         return new ApiResponse(400, "Tên status đã tồn tại", null);
//                     }
//                     status.StatusName = model.StatusName;
//                 }

//                 if (model.Description != null)
//                     status.Description = model.Description;

//                 db.SaveChanges();

//                 return new ApiResponse(200, "Cập nhật user status thành công", null);
//             }
//             catch (Exception e)
//             {
//                 return new ApiResponse(502, e.Message, null);
//             }
//         }

//         public ApiResponse Delete(int id)
//         {
//             try
//             {
//                 var status = db.UserStatuses.FirstOrDefault(s => s.IdUserStatus == id);
//                 if (status == null)
//                 {
//                     return new ApiResponse(404, "Không tìm thấy user status", null);
//                 }

//                 // Check if status is being used by any users
//                 var usersWithStatus = db.Users.Any(u => u.IdUserStatus == id);
//                 if (usersWithStatus)
//                 {
//                     return new ApiResponse(400, "Không thể xóa status đang được sử dụng bởi người dùng", null);
//                 }

//                 db.UserStatuses.Remove(status);
//                 db.SaveChanges();

//                 return new ApiResponse(200, "Xóa user status thành công", null);
//             }
//             catch (Exception e)
//             {
//                 return new ApiResponse(502, e.Message, null);
//             }
//         }

//         public ApiResponse GetActiveStatuses()
//         {
//             try
//             {
//                 var statuses = db.UserStatuses
//                     .OrderBy(s => s.IdUserStatus)
//                     .Select(s => new
//                     {
//                         s.IdUserStatus,
//                         s.StatusName,
//                         s.Description,
//                         UserCount = s.Users.Count()
//                     })
//                     .ToList();

//                 return new ApiResponse(200, "Lấy danh sách active user statuses thành công", statuses);
//             }
//             catch (Exception e)
//             {
//                 return new ApiResponse(502, e.Message, null);
//             }
//         }
//     }
// }
