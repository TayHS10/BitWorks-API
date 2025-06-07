namespace GPP_API.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
        Task SendProjectCreatedEmail(string recipientEmail, GPP_API.DTO.Project.ProjectResponseDTO projectDto);
    }
}
