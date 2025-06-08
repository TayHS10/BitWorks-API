namespace GPP_API.DTO.Project
{
    public class AlertProjectResponseDTO
    {
        public int ProjectId { get; set; } // ID del proyecto
        public string ProjectName { get; set; } = null!; // Nombre del proyecto
        public decimal Budget { get; set; } // Presupuesto total del proyecto
        public decimal RemainingBudget { get; set; } // Presupuesto restante del proyecto
    }
}
