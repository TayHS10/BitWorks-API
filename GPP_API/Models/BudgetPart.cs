using System;
using System.Collections.Generic;

namespace GPP_API.Models;

public partial class BudgetPart
{
    public int BudgetPartId { get; set; }

    public int? ProjectId { get; set; }

    public string PartName { get; set; } = null!;

    public decimal AllocatedAmount { get; set; }

    public decimal RemainingAmount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();

    public virtual Project? Project { get; set; }
}
