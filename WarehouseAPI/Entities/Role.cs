using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RentalCarAPI.Data;

public partial class Role
{
    public int IdRole { get; set; }

    public string RoleName { get; set; } = null!;

    public string? Description { get; set; }

    [JsonIgnore]
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
