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
    public string Status {  get; set; }

    public decimal RemainingBudget { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? ManagerEmail { get; set; }

    public virtual ICollection<Alert> Alerts { get; set; } = new List<Alert>();

    public virtual ICollection<BudgetPart> BudgetParts { get; set; } = new List<BudgetPart>();

    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();

    public virtual User? ManagerEmailNavigation { get; set; }
}
