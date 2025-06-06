using GPP_API.DTO.Alert;
using GPP_API.DTO.BudgetPart;
using GPP_API.DTO.Expense;
using GPP_API.DTO.User;

namespace GPP_API.DTO.Project
{
    public class ProjectDTO
    {
        public int ProjectId { get; set; }
        public string ProjectCode { get; set; } = null!;
        public string ProjectName { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Budget { get; set; }
        public decimal RemainingBudget { get; set; }
        public string Status { get; set; }
        public DateTime? CreatedAt { get; set; }

        public string? ManagerEmail { get; set; }
        public UserDTO? Manager { get; set; }

        public List<AlertDTO> Alerts { get; set; } = new();
        public List<BudgetPartDTO> BudgetParts { get; set; } = new();
        public List<ExpenseDTO> Expenses { get; set; } = new();
    }

}
