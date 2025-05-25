using System;
using System.Collections.Generic;

namespace GPP_API.Models;

public partial class AuditLog
{
    public int LogId { get; set; }

    public int? UserId { get; set; }

    public string ActionType { get; set; } = null!;

    public string? ActionDescription { get; set; }

    public DateTime? ActionDate { get; set; }

    public  User? User { get; set; }
}
