// using Microsoft.EntityFrameworkCore;
// using WarehouseAPI.Data;
// using WarehouseAPI.Helpers;
// using WarehouseAPI.ModelView.User;

// namespace WarehouseAPI.Services.User
// {
//     public class UserServiceOptimized : IUserService
//     {
//         private readonly WarehouseAPIContext db;

//         public UserServiceOptimized(WarehouseAPIContext db)
//         {
//             this.db = db;
//         }

//         public ApiResponse GetAll()
//         {
//             try
//             {
//                 var users = db.Users
//                     .Include(u => u.IdRoleNavigation)
//                     .Include(u => u.IdUserStatusNavigation)
//                     .OrderByDescending(u => u.IdUser)
//                     .Select(u => new UserResponseModel
//                     {
//                         IdUser = u.IdUser,
//                         Email = u.Email,
//                         FullName = u.FullName,
//                         Phone = u.Phone,
//                         Address = u.Address,
//                         IdCardNumber = u.IdCardNumber,
//                         DriverLicense = u.DriverLicense,
//                         DriverLicenseExpiry = u.DriverLicenseExpiry,
//                         IdRole = u.IdRole,
//                         RoleName = u.IdRoleNavigation.RoleName,
//                         IdUserStatus = u.IdUserStatus,
//                         UserStatusName = u.IdUserStatusNavigation.StatusName,
//                         CreatedAt = u.CreatedAt
//                     })
//                     .ToList();
//                 return new ApiResponse(200, "Lấy danh sách người dùng thành công", users);
//             }
//             catch (Exception e)
//             {
//                 return new ApiResponse(502, e.Message, null);
//             }
//         }

//         public ApiResponse GetUsers(string? role, string? status, string? search, int page, int limit)
//         {
//             try
//             {
//                 var query = db.Users
//                     .Include(u => u.IdRoleNavigation)
//                     .Include(u => u.IdUserStatusNavigation)
//                     .AsQueryable();

//                 if (!string.IsNullOrWhiteSpace(role))
//                 {
//                     var r = role.Trim().ToLower();
//                     query = query.Where(u => u.IdRoleNavigation.RoleName.ToLower() == r);
//                 }

//                 if (!string.IsNullOrWhiteSpace(status))
//                 {
//                     var s = status.Trim().ToLower();
//                     query = query.Where(u => u.IdUserStatusNavigation.StatusName.ToLower() == s);
//                 }

//                 if (!string.IsNullOrWhiteSpace(search))
//                 {
//                     var searchTerm = search.Trim().ToLower();
//                     query = query.Where(u => 
//                         u.FullName.ToLower().Contains(searchTerm) ||
//                         u.Email.ToLower().Contains(searchTerm) ||
//                         u.Phone.Contains(searchTerm));
//                 }

//                 var totalCount = query.Count();
//                 var users = query
//                     .OrderByDescending(u => u.IdUser)
//                     .Skip((page - 1) * limit)
//                     .Take(limit)
//                     .ToList();

//                 var result = new
//                 {
//                     TotalCount = totalCount,
//                     Page = page,
//                     Limit = limit,
//                     TotalPages = (int)Math.Ceiling((double)totalCount / limit),
//                     Data = users
//                 };

//                 return new ApiResponse(200, "Lấy danh sách người dùng thành công", result);
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
//                 var user = db.Users
//                     .Include(u => u.IdRoleNavigation)
//                     .Include(u => u.IdUserStatusNavigation)
//                     .Where(u => u.IdUser == id)
//                     .Select(u => new UserResponseModel
//                     {
//                         IdUser = u.IdUser,
//                         Email = u.Email,
//                         FullName = u.FullName,
//                         Phone = u.Phone,
//                         Address = u.Address,
//                         IdCardNumber = u.IdCardNumber,
//                         DriverLicense = u.DriverLicense,
//                         DriverLicenseExpiry = u.DriverLicenseExpiry,
//                         IdRole = u.IdRole,
//                         RoleName = u.IdRoleNavigation.RoleName,
//                         IdUserStatus = u.IdUserStatus,
//                         UserStatusName = u.IdUserStatusNavigation.StatusName,
//                         CreatedAt = u.CreatedAt
//                     })
//                     .FirstOrDefault();

//                 if (user == null)
//                 {
//                     return new ApiResponse(404, "Không tìm thấy người dùng", null);
//                 }

//                 return new ApiResponse(200, "Lấy thông tin người dùng thành công", user);
//             }
//             catch (Exception e)
//             {
//                 return new ApiResponse(502, e.Message, null);
//             }
//         }

//         public ApiResponse Create(UserCreateModel model)
//         {
//             try
//             {
//                 // Check if email already exists
//                 var existingUser = db.Users.FirstOrDefault(u => u.Email == model.Email);
//                 if (existingUser != null)
//                 {
//                     return new ApiResponse(400, "Email đã tồn tại", null);
//                 }

//                 // Check if ID card number already exists
//                 var existingIdCard = db.Users.FirstOrDefault(u => u.IdCardNumber == model.IdCardNumber);
//                 if (existingIdCard != null)
//                 {
//                     return new ApiResponse(400, "Số CMND/CCCD đã tồn tại", null);
//                 }

//                 var user = new Data.User
//                 {
//                     Email = model.Email,
//                     Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
//                     FullName = model.FullName,
//                     Phone = model.Phone,
//                     Address = model.Address,
//                     IdCardNumber = model.IdCardNumber,
//                     DriverLicense = model.DriverLicense,
//                     DriverLicenseExpiry = model.DriverLicenseExpiry,
//                     IdRole = model.IdRole,
//                     IdUserStatus = model.IdUserStatus,
//                     CreatedAt = DateTime.Now
//                 };

//                 db.Users.Add(user);
//                 db.SaveChanges();

//                 return new ApiResponse(201, "Tạo người dùng thành công", user.IdUser);
//             }
//             catch (Exception e)
//             {
//                 return new ApiResponse(502, e.Message, null);
//             }
//         }

//         public ApiResponse Update(int id, UserUpdateModel model)
//         {
//             try
//             {
//                 var user = db.Users.FirstOrDefault(u => u.IdUser == id);
//                 if (user == null)
//                 {
//                     return new ApiResponse(404, "Không tìm thấy người dùng", null);
//                 }

//                 if (!string.IsNullOrEmpty(model.FullName))
//                     user.FullName = model.FullName;

//                 if (!string.IsNullOrEmpty(model.Phone))
//                     user.Phone = model.Phone;

//                 if (!string.IsNullOrEmpty(model.Address))
//                     user.Address = model.Address;

//                 if (!string.IsNullOrEmpty(model.DriverLicense))
//                     user.DriverLicense = model.DriverLicense;

//                 if (model.LicenseExpiryDate.HasValue)
//                     user.DriverLicenseExpiry = model.LicenseExpiryDate.Value;

//                 if (model.IdUserStatus.HasValue)
//                     user.IdUserStatus = model.IdUserStatus.Value;

//                 if (model.IdRole.HasValue)
//                     user.IdRole = model.IdRole.Value;

//                 if (!string.IsNullOrEmpty(model.Password))
//                     user.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);

                
//                 db.SaveChanges();

//                 return new ApiResponse(200, "Cập nhật người dùng thành công", null);
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
//                 var user = db.Users.FirstOrDefault(u => u.IdUser == id);
//                 if (user == null)
//                 {
//                     return new ApiResponse(404, "Không tìm thấy người dùng", null);
//                 }

//                 // Check if user has active bookings
//                 var activeBookings = db.Bookings.Any(b => 
//                     (b.IdCustomer == id || b.IdDriver == id) && 
//                     b.IdBookingStatus == 2); // Confirmed status

//                 if (activeBookings)
//                 {
//                     return new ApiResponse(400, "Không thể xóa người dùng có booking đang hoạt động", null);
//                 }

//                 // Set user status to inactive instead of deleting
//                 user.IdUserStatus = 2; // Inactive
//                 db.SaveChanges();

//                 return new ApiResponse(200, "Vô hiệu hóa người dùng thành công", null);
//             }
//             catch (Exception e)
//             {
//                 return new ApiResponse(502, e.Message, null);
//             }
//         }

//         public ApiResponse GetByRole(int roleId)
//         {
//             try
//             {
//                 var users = db.Users
//                     .Include(u => u.IdRoleNavigation)
//                     .Include(u => u.IdUserStatusNavigation)
//                     .Where(u => u.IdRole == roleId && u.IdUserStatus == 1) // Active users only
//                     .OrderBy(u => u.FullName)
//                     .ToList();

//                 return new ApiResponse(200, "Lấy danh sách người dùng theo vai trò thành công", users);
//             }
//             catch (Exception e)
//             {
//                 return new ApiResponse(502, e.Message, null);
//             }
//         }

//         public ApiResponse GetDrivers(string? search, bool? availableOnly, DateTime? fromDate, DateTime? toDate, int page, int limit)
//         {
//             try
//             {
//                 var query = db.Users
//                     .Include(u => u.IdRoleNavigation)
//                     .Include(u => u.IdUserStatusNavigation)
//                     .Where(u => u.IdRole == 3 && u.IdUserStatus == 1) // Active drivers only
//                     .AsQueryable();

//                 // Enhanced keyword search across multiple fields
//                 if (!string.IsNullOrWhiteSpace(search))
//                 {
//                     var searchTerm = search.Trim().ToLower();
//                     query = query.Where(u =>
//                         u.FullName.ToLower().Contains(searchTerm) ||
//                         u.Email.ToLower().Contains(searchTerm) ||
//                         u.Phone.Contains(searchTerm) ||
//                         (u.DriverLicense != null && u.DriverLicense.ToLower().Contains(searchTerm)));
//                 }

//                 // Kiểm tra booking của tài xế trong khoảng thời gian (loại trừ các tài xế có booking trùng lặp với khoảng yêu cầu)
//                 if (fromDate.HasValue || toDate.HasValue)
//                 {
//                     var from = fromDate ?? DateTime.MinValue;
//                     var to = toDate ?? DateTime.MaxValue;

//                     // Loại trừ tài xế có bất kỳ booking đang xác nhận/đang thực hiện nào mà trùng với khoảng [from, to]
//                     query = query.Where(u => !db.Bookings.Any(b =>
//                         b.IdDriver == u.IdUser &&
//                         (b.IdBookingStatus == 2 || b.IdBookingStatus == 3) &&
//                         b.StartDate <= to &&
//                         b.EndDate >= from
//                     ));
//                 }

//                 // Driver availability filtering
//                 if (availableOnly.HasValue && availableOnly.Value)
//                 {
//                     // Check if driver is not currently assigned to active bookings
//                     query = query.Where(u => !db.Bookings.Any(b =>
//                         b.IdDriver == u.IdUser &&
//                         (b.IdBookingStatus == 2 || b.IdBookingStatus == 3))); // Confirmed or In Progress
//                 }

//                 var totalCount = query.Count();
//                 var drivers = query
//                     .OrderBy(u => u.FullName)
//                     .Skip((page - 1) * limit)
//                     .Take(limit)
//                     .Select(u => new UserResponseModel
//                     {
//                         IdUser = u.IdUser,
//                         Email = u.Email,
//                         FullName = u.FullName,
//                         Phone = u.Phone,
//                         Address = u.Address,
//                         IdCardNumber = u.IdCardNumber,
//                         DriverLicense = u.DriverLicense,
//                         DriverLicenseExpiry = u.DriverLicenseExpiry,
//                         IdRole = u.IdRole,
//                         RoleName = u.IdRoleNavigation.RoleName,
//                         IdUserStatus = u.IdUserStatus,
//                         UserStatusName = u.IdUserStatusNavigation.StatusName,
//                         CreatedAt = u.CreatedAt
//                     })
//                     .ToList();

//                 var result = new
//                 {
//                     TotalCount = totalCount,
//                     Page = page,
//                     Limit = limit,
//                     TotalPages = (int)Math.Ceiling((double)totalCount / limit),
//                     Data = drivers,
//                     SearchCriteria = new
//                     {
//                         Search = search,
//                         AvailableOnly = availableOnly,
//                         FromDate = fromDate?.ToString("yyyy-MM-dd"),
//                         ToDate = toDate?.ToString("yyyy-MM-dd")
//                     }
//                 };

//                 return new ApiResponse(200, "Lấy danh sách drivers thành công", result);
//             }
//             catch (Exception e)
//             {
//                 return new ApiResponse(502, e.Message, null);
//             }
//         }

//         public ApiResponse GetCustomers(string? search, int page, int limit)
//         {
//             try
//             {
//                 var query = db.Users
//                     .Include(u => u.IdRoleNavigation)
//                     .Include(u => u.IdUserStatusNavigation)
//                     .Where(u => u.IdRole == 2) // Customer role
//                     .AsQueryable();

//                 if (!string.IsNullOrWhiteSpace(search))
//                 {
//                     var searchTerm = search.Trim().ToLower();
//                     query = query.Where(u =>
//                         u.FullName.ToLower().Contains(searchTerm) ||
//                         u.Email.ToLower().Contains(searchTerm) ||
//                         u.Phone.Contains(searchTerm));
//                 }

//                 var totalCount = query.Count();
//                 var customers = query
//                     .OrderByDescending(u => u.IdUser)
//                     .Skip((page - 1) * limit)
//                     .Take(limit)
//                     .ToList();

//                 var result = new
//                 {
//                     TotalCount = totalCount,
//                     Page = page,
//                     Limit = limit,
//                     TotalPages = (int)Math.Ceiling((double)totalCount / limit),
//                     Data = customers
//                 };

//                 return new ApiResponse(200, "Lấy danh sách customers thành công", result);
//             }
//             catch (Exception e)
//             {
//                 return new ApiResponse(502, e.Message, null);
//             }
//         }
//     }
// }
