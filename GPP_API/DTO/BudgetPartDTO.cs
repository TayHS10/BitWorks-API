namespace GPP_API.DTO
{
    public class BudgetPartDTO
    {
        public int BudgetPartId { get; set; }
        public string PartName { get; set; } = null!;
        public decimal AllocatedAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public DateTime? CreatedAt { get; set; }

        public List<ExpenseDTO> Expenses { get; set; } = new();
    }

}
