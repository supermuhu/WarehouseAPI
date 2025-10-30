using Microsoft.EntityFrameworkCore;
using WarehouseAPI.Data;
using WarehouseAPI.Helpers;
using WarehouseAPI.ModelView.Role;

namespace WarehouseAPI.Services.Role
{
    public class RoleService : IRoleService
    {
        private readonly WarehouseAPIContext db;

        public RoleService(WarehouseAPIContext db)
        {
            this.db = db;
        }

        public ApiResponse GetAll()
        {
            try
            {
                var roles = db.Roles
                    .OrderBy(r => r.IdRole)
                    .ToList();

                return new ApiResponse(200, "Lấy danh sách roles thành công", roles);
            }
            catch (Exception e)
            {
                return new ApiResponse(502, e.Message, null);
            }
        }

        public ApiResponse GetById(int id)
        {
            try
            {
                var role = db.Roles.FirstOrDefault(r => r.IdRole == id);

                if (role == null)
                {
                    return new ApiResponse(404, "Không tìm thấy role", null);
                }

                return new ApiResponse(200, "Lấy thông tin role thành công", role);
            }
            catch (Exception e)
            {
                return new ApiResponse(502, e.Message, null);
            }
        }

        public ApiResponse Create(RoleCreateModel model)
        {
            try
            {
                // Check if role name already exists
                var existingRole = db.Roles.FirstOrDefault(r => r.RoleName.ToLower() == model.RoleName.ToLower());
                if (existingRole != null)
                {
                    return new ApiResponse(400, "Tên role đã tồn tại", null);
                }

                var role = new Data.Role
                {
                    RoleName = model.RoleName,
                    Description = model.Description
                };

                db.Roles.Add(role);
                db.SaveChanges();

                return new ApiResponse(201, "Tạo role thành công", role.IdRole);
            }
            catch (Exception e)
            {
                return new ApiResponse(502, e.Message, null);
            }
        }

        public ApiResponse Update(int id, RoleUpdateModel model)
        {
            try
            {
                var role = db.Roles.FirstOrDefault(r => r.IdRole == id);
                if (role == null)
                {
                    return new ApiResponse(404, "Không tìm thấy role", null);
                }

                if (!string.IsNullOrEmpty(model.RoleName))
                {
                    // Check if new role name already exists (excluding current role)
                    var existingRole = db.Roles.FirstOrDefault(r => 
                        r.RoleName.ToLower() == model.RoleName.ToLower() && r.IdRole != id);
                    if (existingRole != null)
                    {
                        return new ApiResponse(400, "Tên role đã tồn tại", null);
                    }
                    role.RoleName = model.RoleName;
                }

                if (model.Description != null)
                    role.Description = model.Description;

                db.SaveChanges();

                return new ApiResponse(200, "Cập nhật role thành công", null);
            }
            catch (Exception e)
            {
                return new ApiResponse(502, e.Message, null);
            }
        }

        public ApiResponse Delete(int id)
        {
            try
            {
                var role = db.Roles.FirstOrDefault(r => r.IdRole == id);
                if (role == null)
                {
                    return new ApiResponse(404, "Không tìm thấy role", null);
                }

                // Check if role is being used by any users
                var usersWithRole = db.Users.Any(u => u.IdRole == id);
                if (usersWithRole)
                {
                    return new ApiResponse(400, "Không thể xóa role đang được sử dụng bởi người dùng", null);
                }

                db.Roles.Remove(role);
                db.SaveChanges();

                return new ApiResponse(200, "Xóa role thành công", null);
            }
            catch (Exception e)
            {
                return new ApiResponse(502, e.Message, null);
            }
        }

        public ApiResponse GetActiveRoles()
        {
            try
            {
                var roles = db.Roles
                    .OrderBy(r => r.IdRole)
                    .Select(r => new
                    {
                        r.IdRole,
                        r.RoleName,
                        r.Description,
                        UserCount = r.Users.Count()
                    })
                    .ToList();

                return new ApiResponse(200, "Lấy danh sách active roles thành công", roles);
            }
            catch (Exception e)
            {
                return new ApiResponse(502, e.Message, null);
            }
        }
    }
}
