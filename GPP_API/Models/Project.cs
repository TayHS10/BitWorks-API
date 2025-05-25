using System;
using System.Collections.Generic;

namespace GPP_API.Models;

public partial class Project
{
    public int ProjectId { get; set; }

    public string ProjectCode { get; set; } = null!;

    public string ProjectName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Budget { get; set; }

    public decimal RemainingBudget { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? ManagerEmail { get; set; }

}
