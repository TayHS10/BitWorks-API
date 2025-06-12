namespace GPP_API.DTO.Project
{
    public class AlertProjectResponseDTO
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = null!;
        public decimal Budget { get; set; }
        public decimal RemainingBudget { get; set; }
    }
}
