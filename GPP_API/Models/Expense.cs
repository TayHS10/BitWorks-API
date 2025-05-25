using System;
using System.Collections.Generic;

namespace GPP_API.Models;

public partial class Expense
{
    public int ExpenseId { get; set; }

    public int? ProjectId { get; set; }

    public int? BudgetPartId { get; set; }

    public decimal ExpenseAmount { get; set; }

    public DateOnly ExpenseDate { get; set; }

    public string? DocumentReference { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

}
