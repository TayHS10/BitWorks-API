using System.ComponentModel.DataAnnotations;

namespace GPP_API.DTO.BudgetPart
{
    public class DeactivateBudgetPartDTO
    {
        public int BudgetPartIdToDeactivate { get; set; }

        public int ReceivingBudgetPartId { get; set; }
    }
}
