using GPP_API.DTO.BudgetPart;

namespace GPP_API.DTO.Project
{
    public class CreateProjectDTO
    {
        public string ProjectCode { get; set; } = null!;
        public string ProjectName { get; set; } = null!;
        public string? Description { get; set; }
        public string ManagerEmail { get; set; } = null!;

        public List<CreateBudgetPartDTO> BudgetParts { get; set; } = new List<CreateBudgetPartDTO>();
    }

}
