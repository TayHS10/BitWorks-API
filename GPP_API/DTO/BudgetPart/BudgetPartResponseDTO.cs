using GPP_API.DTO.Expense;

namespace GPP_API.DTO.BudgetPart
{
    public class BudgetPartResponseDTO
    {
        public int BudgetPartId { get; set; }
        public string PartName { get; set; } = null!;
        public decimal AllocatedAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public DateTime? CreatedAt { get; set; }
        public List<ExpenseResponseDTO> Expenses { get; set; } = new();
    }

}
