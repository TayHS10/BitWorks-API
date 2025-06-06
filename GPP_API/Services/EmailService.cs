using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using MailKit.Net.Smtp;

namespace GPP_API.Services
{
    public class EmailService : IEmailService // Implementa la interfaz
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            // Obtener la configuración SMTP desde appsettings.json
            var smtpHost = _configuration["SmtpSettings:Host"];
            var smtpPort = int.Parse(_configuration["SmtpSettings:Port"]);
            var smtpUsername = _configuration["SmtpSettings:Username"];
            var smtpPassword = _configuration["SmtpSettings:Password"];
            var fromEmail = _configuration["SmtpSettings:FromEmail"];
            var fromName = _configuration["SmtpSettings:FromName"] ?? "Gestion Presupuestaria Proyectos"; // Nombre predeterminado

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword) || string.IsNullOrEmpty(fromEmail))
            {
                throw new InvalidOperationException("La configuración SMTP no está completa en appsettings.json.");
            }

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(fromName, fromEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = message };

            using var smtp = new SmtpClient();
            try
            {
                await smtp.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(smtpUsername, smtpPassword);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // Aquí deberías usar un sistema de logging real (ej. Serilog, NLog, ILogger)
                Console.WriteLine($"Error al enviar correo a {toEmail}: {ex.Message}");
                // En un entorno de producción, puedes decidir loggear el error
                // y no relanzar la excepción si el envío de correo no debe bloquear la respuesta al usuario,
                // pero para un restablecimiento de contraseña, es crítico.
                throw new InvalidOperationException($"No se pudo enviar el correo de restablecimiento de contraseña. Detalles: {ex.Message}", ex);
            }
        }
    }
}
