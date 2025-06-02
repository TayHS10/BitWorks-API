using System;
using System.Collections.Generic;

namespace GPP_API.Models;

public partial class Alert
{
    public int AlertId { get; set; }

    public int? ProjectId { get; set; }

    public string? AlertType { get; set; }

    public string Message { get; set; } = null!;
    public string Status { get; set; }

    public DateTime? AlertDate { get; set; }

    public virtual Project? Project { get; set; }
}
