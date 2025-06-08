namespace GPP_API.DTO.BudgetPart
{
    public class AlertBudgetPartDTO
    {
        public int BudgetPartId { get; set; } // ID de la partida presupuestaria
        public string PartName { get; set; } = null!; // Nombre de la partida presupuestaria
        public decimal AllocatedAmount { get; set; } // Monto total asignado
        public decimal RemainingAmount { get; set; } // Monto restante
    }
}
