using System;
using System.Collections.Generic;

namespace GPP_API.Models;

public partial class RoleChangeRequest
{
    public int RequestId { get; set; }
    public required string RequestedRole { get; set; } 
    public string Justification { get; set; }
    public required string EmailAddress { get; set; }
    public required string FullName { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }

    // Propiedad de navegación
    public virtual User UserEmailNavigation { get; set; } = null!;
}
