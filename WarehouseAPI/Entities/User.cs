using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RentalCarAPI.Data;

public partial class User
{
    public int IdUser { get; set; }

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string IdCardNumber { get; set; } = null!;

    public string? DriverLicense { get; set; }

    public DateTime? DriverLicenseExpiry { get; set; }

    public int IdRole { get; set; }

    public int IdUserStatus { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Role IdRoleNavigation { get; set; } = null!;
}
