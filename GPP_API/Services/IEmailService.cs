using GPP_API.DTO.BudgetPart;
using GPP_API.DTO.Project;

namespace GPP_API.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
        Task SendProjectCreatedEmail(string recipientEmail, GPP_API.DTO.Project.ProjectResponseDTO projectDto);
        Task SendProjectBudgetPercentageEmail(string recipientEmail, AlertProjectResponseDTO projectDto, decimal percentage);
        Task SendBudgetPartPercentageEmail(string recipientEmail, AlertBudgetPartDTO budgetPartDto, decimal percentage);
    }
}
