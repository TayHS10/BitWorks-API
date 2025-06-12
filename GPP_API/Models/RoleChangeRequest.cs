using System;
using System.Collections.Generic;

namespace GPP_API.Models;

public partial class RoleChangeRequest
{
    public int RequestId { get; set; }

    public string? UserEmail { get; set; }

    public string? RequestedRole { get; set; }

    public string? Justification { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? UserEmailNavigation { get; set; }
}
