namespace GPP_API.DTO.BudgetPart
{
    public class AlertBudgetPartDTO
    {
        public int BudgetPartId { get; set; }
        public string PartName { get; set; } = null!;
        public decimal AllocatedAmount { get; set; }
        public decimal RemainingAmount { get; set; }
    }
}
