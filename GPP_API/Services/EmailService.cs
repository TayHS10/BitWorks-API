using GPP_API.DTO.BudgetPart;
using GPP_API.DTO.Project;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace GPP_API.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            var smtpHost = _configuration["SmtpSettings:Host"];
            var smtpPort = int.Parse(_configuration["SmtpSettings:Port"] ?? "0");
            var smtpUsername = _configuration["SmtpSettings:Username"];
            var smtpPassword = _configuration["SmtpSettings:Password"];
            var fromEmail = _configuration["SmtpSettings:FromEmail"];
            var fromName = _configuration["SmtpSettings:FromName"] ?? "Gestion Presupuestaria Proyectos";

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword) || string.IsNullOrEmpty(fromEmail) || smtpPort == 0)
            {
                _logger.LogError("SMTP configuration is incomplete or invalid in appsettings.json.");
                throw new InvalidOperationException("La configuración SMTP no está completa o es inválida en appsettings.json.");
            }

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(fromName, fromEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = message };

            using var smtp = new SmtpClient();
            try
            {
                _logger.LogInformation("Attempting to connect to SMTP host: {SmtpHost}:{SmtpPort}", smtpHost, smtpPort);
                await smtp.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                _logger.LogInformation("Connected to SMTP host. Attempting authentication for user: {SmtpUsername}", smtpUsername);
                await smtp.AuthenticateAsync(smtpUsername, smtpPassword);
                _logger.LogInformation("Authenticated with SMTP server. Sending email to: {ToEmail} with subject: {Subject}", toEmail, subject);
                await smtp.SendAsync(email);
                _logger.LogInformation("Email sent successfully to: {ToEmail}", toEmail);
                await smtp.DisconnectAsync(true);
                _logger.LogInformation("Disconnected from SMTP server.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {ToEmail}: {ErrorMessage}", toEmail, ex.Message);
                throw new InvalidOperationException($"No se pudo enviar el correo a {toEmail}. Detalles: {ex.Message}", ex);
            }
        }

        public async Task SendProjectCreatedEmail(string recipientEmail, ProjectResponseDTO projectDto)
        {
            var projectName = projectDto.ProjectName;
            var totalBudget = projectDto.Budget;
            var budgetParts = projectDto.BudgetParts;
            var createdAt = projectDto.CreatedAt ?? DateTime.Now;

            var subject = $"Confirmación: Proyecto '{projectName}' Creado Exitosamente";

            // Build the budget parts table rows dynamically
            var budgetPartsTableRows = "";
            if (budgetParts != null && budgetParts.Any())
            {
                foreach (var bp in budgetParts)
                {
                    budgetPartsTableRows += $@"
                        <tr>
                            <td width=""50%"" style=""vertical-align: top; padding: 10px; word-break: break-word; border-top: 1px solid #dddddd; border-right: 1px solid #dddddd; border-bottom: 1px solid #dddddd; border-left: 1px solid #dddddd;"">{bp.PartName}</td>
                            <td width=""50%"" style=""vertical-align: top; padding: 10px; word-break: break-word; border-top: 1px solid #dddddd; border-right: 1px solid #dddddd; border-bottom: 1px solid #dddddd; border-left: 1px solid #dddddd;"">₡{bp.AllocatedAmount.ToString("N2", System.Globalization.CultureInfo.InvariantCulture)}</td>
                        </tr>";
                }
            }
            else
            {
                budgetPartsTableRows += $@"
                    <tr>
                        <td colspan=""2"" width=""100%"" style=""vertical-align: top; padding: 10px; word-break: break-word; border-top: 1px solid #dddddd; border-right: 1px solid #dddddd; border-bottom: 1px solid #dddddd; border-left: 1px solid #dddddd; text-align: center;"">No se especificaron partidas presupuestarias.</td>
                    </tr>";
            }

            // Construct the full HTML message using the provided template
            var message = $@"
<!DOCTYPE html>
<html xmlns:v=""urn:schemas-microsoft-com:vml"" xmlns:o=""urn:schemas-microsoft-com:office:office"" lang=""en"">

<head>
    <title>Proyecto Creado - {projectName}</title>
    <meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0""><style>
        * {{
            box-sizing: border-box;
        }}

        body {{
            margin: 0;
            padding: 0;
        }}

        a[x-apple-data-detectors] {{
            color: inherit !important;
            text-decoration: inherit !important;
        }}

        #MessageViewBody a {{
            color: inherit;
            text-decoration: none;
        }}

        p {{
            line-height: inherit
        }}

        .desktop_hide,
        .desktop_hide table {{
            mso-hide: all;
            display: none;
            max-height: 0px;
            overflow: hidden;
        }}

        .image_block img+div {{
            display: none;
        }}

        sup,
        sub {{
            font-size: 75%;
            line-height: 0;
        }}

        @media (max-width:620px) {{
            .desktop_hide table.icons-inner {{
                display: inline-block !important;
            }}

            .icons-inner {{
                text-align: center;
            }}

            .icons-inner td {{
                margin: 0 auto;
            }}

            .mobile_hide {{
                display: none;
            }}

            .row-content {{
                width: 100% !important;
            }}

            .stack .column {{
                width: 100%;
                display: block;
            }}

            .mobile_hide {{
                min-height: 0;
                max-height: 0;
                max-width: 0;
                overflow: hidden;
                font-size: 0px;
            }}

            .desktop_hide,
            .desktop_hide table {{
                display: table !important;
                max-height: none !important;
            }}
        }}
    </style></head>

<body class=""body"" style=""background-color: #ffffff; margin: 0; padding: 0; -webkit-text-size-adjust: none; text-size-adjust: none;"">
    <table class=""nl-container"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; background-color: #ffffff;"">
        <tbody>
            <tr>
                <td>
                    <table class=""row row-1"" align=""center"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
                        <tbody>
                            <tr>
                                <td>
                                    <table class=""row-content stack"" align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; color: #000000; width: 600px; margin: 0 auto;"" width=""600"">
                                        <tbody>
                                            <tr>
                                                <td class=""column column-1"" width=""100%"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; font-weight: 400; text-align: left; padding-bottom: 5px; padding-top: 5px; vertical-align: top;"">
                                                    <table class=""heading_block block-1"" width=""100%"" border=""0"" cellpadding=""10"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
                                                        <tr>
                                                            <td class=""pad"">
                                                                <h1 style=""margin: 0; color: #3a889d; direction: ltr; font-family: Arial, Helvetica, sans-serif; font-size: 38px; font-weight: 700; letter-spacing: normal; line-height: 1.2; text-align: left; margin-top: 0; margin-bottom: 0; mso-line-height-alt: 46px;""><span class=""tinyMce-placeholder"" style=""word-break: break-word;""><strong>CREACIÓN DEL PROYECTO:</strong></span></h1>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                            </tr>
                                        </tbody>
                                    </table>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                    <table class=""row row-2"" align=""center"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
                        <tbody>
                            <tr>
                                <td>
                                    <table class=""row-content stack"" align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-radius: 0; color: #000000; width: 600px; margin: 0 auto;"" width=""600"">
                                        <tbody>
                                            <tr>
                                                <td class=""column column-1"" width=""100%"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; font-weight: 400; text-align: left; padding-bottom: 5px; padding-top: 5px; vertical-align: top;"">
                                                    <table class=""heading_block block-1"" width=""100%"" border=""0"" cellpadding=""10"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
                                                        <tr>
                                                            <td class=""pad"">
                                                                <h1 style=""margin: 0; color: #3a889d; direction: ltr; font-family: Arial, Helvetica, sans-serif; font-size: 38px; font-weight: 700; letter-spacing: normal; line-height: 1.2; text-align: left; margin-top: 0; margin-bottom: 0; mso-line-height-alt: 46px;""><span class=""tinyMce-placeholder"" style=""word-break: break-word;"">{projectName}</span></h1>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                    <table class=""divider_block block-2"" width=""100%"" border=""0"" cellpadding=""10"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
                                                        <tr>
                                                            <td class=""pad"">
                                                                <div class=""alignment"" align=""center"">
                                                                    <table border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" width=""100%"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
                                                                        <tr>
                                                                            <td class=""divider_inner"" style=""font-size: 1px; line-height: 1px; border-top: 1px solid #dddddd;""><span style=""word-break: break-word;"">&#8202;</span></td>
                                                                        </tr>
                                                                    </table>
                                                                </div>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                    <table class=""paragraph_block block-3"" width=""100%"" border=""0"" cellpadding=""10"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; word-break: break-word;"">
                                                        <tr>
                                                            <td class=""pad"">
                                                                <div style=""color:#393d47;direction:ltr;font-family:Arial,Helvetica,sans-serif;font-size:16px;font-weight:400;line-height:120%;text-align:left;mso-line-height-alt:19.2px;"">
                                                                    <p style=""margin: 0; margin-bottom: 10px;"">Estimado/a Manager,</p>
                                                                    <p style=""margin: 0; margin-bottom: 10px;"">Nos complace informarle que el proyecto <strong>'{projectName}'</strong> ha sido creado en el sistema con los siguientes detalles:</p>
                                                                    <p style=""margin: 0;""><strong>Código de Proyecto:</strong> {projectDto.ProjectCode}</p>
                                                                    <p style=""margin: 0;""><strong>Descripción:</strong> {projectDto.Description ?? "No proporcionada"}</p>   
                                                                </div>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                            </tr>
                                        </tbody>
                                    </table>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                    <table class=""row row-3"" align=""center"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
                        <tbody>
                            <tr>
                                <td>
                                    <table class=""row-content stack"" align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-radius: 0; color: #000000; width: 600px; margin: 0 auto;"" width=""600"">
                                        <tbody>
                                            <tr>
                                                <td class=""column column-1"" width=""100%"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; font-weight: 400; text-align: left; padding-bottom: 5px; padding-top: 5px; vertical-align: top;"">
                                                    <table class=""table_block block-1"" width=""100%"" border=""0"" cellpadding=""10"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
                                                        <tr>
                                                            <td class=""pad"">
                                                                <table style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-collapse: collapse; width: 100%; table-layout: fixed; direction: ltr; background-color: transparent; font-family: Arial, Helvetica, sans-serif; font-weight: 400; color: #101112; text-align: left; letter-spacing: 0px;"" width=""100%"">
                                                                    <thead style=""vertical-align: top; background-color: #f2f2f2; color: #101112; font-size: 14px; line-height: 1.2; mso-line-height-alt: 17px;"">
                                                                        <tr>
                                                                            <th width=""50%"" style=""padding: 10px; word-break: break-word; font-weight: 700; border-top: 1px solid #dddddd; border-right: 1px solid #dddddd; border-bottom: 1px solid #dddddd; border-left: 1px solid #dddddd; text-align: center;"">PARTIDA PRESUPUESTARIA</th>
                                                                            <th width=""50%"" style=""padding: 10px; word-break: break-word; font-weight: 700; border-top: 1px solid #dddddd; border-right: 1px solid #dddddd; border-bottom: 1px solid #dddddd; border-left: 1px solid #dddddd; text-align: center;"">MONTO ASIGNADO</th>
                                                                        </tr>
                                                                    </thead>
                                                                    <tbody style=""vertical-align: top; font-size: 16px; line-height: 1.2; mso-line-height-alt: 19px;"">
                                                                        {budgetPartsTableRows}
                                                                    </tbody>
                                                                </table>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                    <table class=""divider_block block-2"" width=""100%"" border=""0"" cellpadding=""10"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
                                                        <tr>
                                                            <td class=""pad"">
                                                                <div class=""alignment"" align=""center"">
                                                                    <table border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" width=""100%"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
                                                                        <tr>
                                                                            <td class=""divider_inner"" style=""font-size: 1px; line-height: 1px; border-top: 1px solid #dddddd;""><span style=""word-break: break-word;"">&#8202;</span></td>
                                                                        </tr>
                                                                    </table>
                                                                </div>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                            </tr>
                                        </tbody>
                                    </table>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                    <table class=""row row-4"" align=""center"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
                        <tbody>
                            <tr>
                                <td>
                                    <table class=""row-content stack"" align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-radius: 0; color: #000000; width: 600px; margin: 0 auto;"" width=""600"">
                                        <tbody>
                                            <tr>
                                                <td class=""column column-1"" width=""100%"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; font-weight: 400; text-align: left; padding-bottom: 5px; padding-top: 5px; vertical-align: top;"">
                                                    <table class=""heading_block block-1"" width=""100%"" border=""0"" cellpadding=""10"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
                                                        <tr>
                                                            <td class=""pad"">
                                                                <h1 style=""margin: 0; color: #3a889d; direction: ltr; font-family: Arial, Helvetica, sans-serif; font-size: 38px; font-weight: 700; letter-spacing: normal; line-height: 1.2; text-align: left; margin-top: 0; margin-bottom: 0; mso-line-height-alt: 46px;""><span class=""tinyMce-placeholder"" style=""word-break: break-word;"">PRESUPUESTO ASIGNADO: ₡{totalBudget.ToString("N2", System.Globalization.CultureInfo.InvariantCulture)}</span></h1>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                            </tr>
                                        </tbody>
                                    </table>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                    <table class=""row row-6"" align=""center"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
                        <tbody>
                            <tr>
                                <td>
                                    <table class=""row-content stack"" align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-radius: 0; color: #000000; width: 600px; margin: 0 auto;"" width=""600"">
                                        <tbody>
                                            <tr>
                                                <td class=""column column-1"" width=""100%"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; font-weight: 400; text-align: left; padding-bottom: 5px; padding-top: 5px; vertical-align: top;"">
                                                    <table class=""image_block block-1"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
                                                        <tr>
                                                            <td class=""pad"" style=""width:100%;"">
                                                                <div class=""alignment"" align=""center"">
                                                                    <div style=""max-width: 574px;""><img src=""https://members.oeglobal.org/media/logos/firma-horizontal-dos-lineas-cmky.png"" style=""display: block; height: auto; border: 0; width: 100%;"" width=""574"" alt="""" title="""" height=""auto""></div>
                                                                </div>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                            </tr>
                                        </tbody>
                                    </table>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                    <table class=""row row-7"" align=""center"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; background-color: #ffffff;"">
                        <tbody>
                            <tr>
                                <td>
                                    <table class=""row-content stack"" align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; color: #000000; width: 600px; margin: 0 auto;"" width=""600"">
                                        <tbody>
                                            <tr>
                                                <td class=""column column-1"" width=""100%"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; font-weight: 400; text-align: left; padding-bottom: 5px; padding-top: 5px; vertical-align: top;"">
                                                    <table class=""icons_block block-1"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; text-align: center; line-height: 0;"">
                                                        <tr>
                                                            <td class=""pad"" style=""vertical-align: middle; color: #1e0e4b; font-family: 'Inter', sans-serif; font-size: 15px; padding-bottom: 5px; padding-top: 5px; text-align: center;""><table class=""icons-inner"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; display: inline-block; padding-left: 0px; padding-right: 0px;"" cellpadding=""0"" cellspacing=""0"" role=""presentation""><tr>
                                                                        <td style=""vertical-align: middle; text-align: center; padding-top: 5px; padding-bottom: 5px; padding-left: 5px; padding-right: 6px;""><a href=""http://designedwithbeefree.com/"" target=""_blank"" style=""text-decoration: none;""><img class=""icon"" alt=""Beefree Logo"" src=""https://d1oco4z2z1fhwp.cloudfront.net/assets/Beefree-logo.png"" height=""auto"" width=""34"" align=""center"" style=""display: block; height: auto; margin: 0 auto; border: 0;""></a></td>
                                                                        <td style=""font-family: 'Inter', sans-serif; font-size: 15px; font-weight: undefined; color: #1e0e4b; vertical-align: middle; letter-spacing: undefined; text-align: center; line-height: normal;""><a href=""http://designedwithbeefree.com/"" target=""_blank"" style=""color: #1e0e4b; text-decoration: none;"">Designed with Beefree</a></td>
                                                                    </tr>
                                                                </table>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                            </tr>
                                        </tbody>
                                    </table>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </td>
            </tr>
        </tbody>
    </table></body>

</html>
";

            await SendEmailAsync(recipientEmail, subject, message);
        }


        public async Task SendProjectBudgetPercentageEmail(string recipientEmail, AlertProjectResponseDTO projectDto, decimal percentage)
        {
            var projectName = projectDto.ProjectName;
            var totalBudget = projectDto.Budget;
            var remainingBudget = projectDto.RemainingBudget;
            var usedBudget = totalBudget - remainingBudget;
            var currentPercentage = (usedBudget / totalBudget) * 100;

            if (currentPercentage >= percentage)
            {
                var subject = $"Alerta de Presupuesto: '{projectName}' ha alcanzado el {currentPercentage:N2}%";

                var message = $@"
<!DOCTYPE html>
<html xmlns:v=""urn:schemas-microsoft-com:vml"" xmlns:o=""urn:schemas-microsoft-com:office:office"" lang=""en"">
<head>
    <title></title>
    <meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        * {{
            box-sizing: border-box;
        }}

        body {{
            margin: 0;
            padding: 0;
        }}

        a[x-apple-data-detectors] {{
            color: inherit !important;
            text-decoration: inherit !important;
        }}

        #MessageViewBody a {{
            color: inherit;
            text-decoration: none;
        }}

        p {{
            line-height: inherit
        }}

        .desktop_hide,
        .desktop_hide table {{
            mso-hide: all;
            display: none;
            max-height: 0px;
            overflow: hidden;
        }}

        .image_block img+div {{
            display: none;
        }}

        sup,
        sub {{
            font-size: 75%;
            line-height: 0;
        }}

        @media (max-width:620px) {{
            .desktop_hide table.icons-inner {{
                display: inline-block !important;
            }}

            .icons-inner {{
                text-align: center;
            }}

            .icons-inner td {{
                margin: 0 auto;
            }}

            .mobile_hide {{
                display: none;
            }}

            .row-content {{
                width: 100% !important;
            }}

            .stack .column {{
                width: 100%;
                display: block;
            }}

            .mobile_hide {{
                min-height: 0;
                max-height: 0;
                max-width: 0;
                overflow: hidden;
                font-size: 0px;
            }}

            .desktop_hide,
            .desktop_hide table {{
                display: table !important;
                max-height: none !important;
            }}
        }}
    </style>
</head>

<body class=""body"" style=""background-color: #ffffff; margin: 0; padding: 0; -webkit-text-size-adjust: none; text-size-adjust: none;"">
    <table class=""nl-container"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; background-color: #ffffff;"">
        <tbody>
            <tr>
                <td>
                    <table class=""row row-1"" align=""center"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
                        <tbody>
                            <tr>
                                <td>
                                    <table class=""row-content stack"" align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; color: #000000; width: 600px; margin: 0 auto;"" width=""600"">
                                        <tbody>
                                            <tr>
                                                <td class=""column column-1"" width=""100%"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; font-weight: 400; text-align: left; padding-bottom: 5px; padding-top: 5px; vertical-align: top;"">
                                                    <table class=""heading_block block-1"" width=""100%"" border=""0"" cellpadding=""10"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
                                                        <tr>
                                                            <td class=""pad"">
                                                                <h1 style=""margin: 0; color: #3a889d; direction: ltr; font-family: Arial, Helvetica, sans-serif; font-size: 38px; font-weight: 700; letter-spacing: normal; line-height: 1.2; text-align: left; margin-top: 0; margin-bottom: 0; mso-line-height-alt: 46px;""><span class=""tinyMce-placeholder"" style=""word-break: break-word;""><u><strong>¡ALERTA DE GESTIÓN PRESUPUESTARIA!</strong></u></span></h1>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                            </tr>
                                        </tbody>
                                    </table>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                    <table class=""row row-2"" align=""center"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
                        <tbody>
                            <tr>
                                <td>
                                    <table class=""row-content stack"" align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-radius: 0; color: #000000; width: 600px; margin: 0 auto;"" width=""600"">
                                        <tbody>
                                            <tr>
                                                <td class=""column column-1"" width=""100%"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; font-weight: 400; text-align: left; padding-bottom: 5px; padding-top: 5px; vertical-align: top;"">
                                                    <table class=""heading_block block-1"" width=""100%"" border=""0"" cellpadding=""10"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
                                                        <tr>
                                                            <td class=""pad"">
                                                                <h1 style=""margin: 0; color: #3a889d; direction: ltr; font-family: Arial, Helvetica, sans-serif; font-size: 38px; font-weight: 700; letter-spacing: normal; line-height: 1.2; text-align: left; margin-top: 0; margin-bottom: 0; mso-line-height-alt: 46px;""><span class=""tinyMce-placeholder"" style=""word-break: break-word;"">Estimado(a) Manager,</span></h1>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                            </tr>
                                        </tbody>
                                    </table>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                    <table class=""row row-3"" align=""center"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
                        <tbody>
                            <tr>
                                <td>
                                    <table class=""row-content stack"" align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-radius: 0; color: #000000; width: 600px; margin: 0 auto;"" width=""600"">
                                        <tbody>
                                            <tr>
                                                <td class=""column column-1"" width=""100%"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; font-weight: 400; text-align: left; padding-bottom: 5px; padding-top: 5px; vertical-align: top;"">
                                                    <table class=""paragraph_block block-1"" width=""100%"" border=""0"" cellpadding=""10"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; word-break: break-word;"">
                                                        <tr>
                                                            <td class=""pad"">
                                                                <div style=""color:#101112;direction:ltr;font-family:Arial, Helvetica, sans-serif;font-size:16px;font-weight:400;letter-spacing:0px;line-height:1.2;text-align:left;mso-line-height-alt:19px;"">
                                                                    <p style=""margin: 0; margin-bottom: 10px;"">Le informamos que el proyecto <strong>'{projectName}'</strong> ha utilizado el <strong>{currentPercentage:N2}%</strong> de su presupuesto total.</p>
                                                                    <p style=""margin: 0; margin-bottom: 10px;"">A continuación, se detalla el estado actual del presupuesto:</p>
                                                                    <p style=""margin: 0;""><strong>Presupuesto Total Asignado:</strong> ₡{totalBudget:N2}</p>
                                                                    <p style=""margin: 0;""><strong>Monto Utilizado:</strong> ₡{usedBudget:N2}</p>
                                                                    <p style=""margin: 0;""><strong>Presupuesto Restante:</strong> ₡{remainingBudget:N2}</p>
                                                                    <p style=""margin: 0; margin-top: 15px;"">Por favor, revise esta situación y tome las medidas necesarias para una gestión presupuestaria óptima del proyecto.</p>
                                                                    <p style=""margin: 0; margin-top: 15px;"">Saludos cordiales,</p>
                                                                    <p style=""margin: 0;"">El Equipo de Gestión de Proyectos</p>
                                                                </div>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                            </tr>
                                        </tbody>
                                    </table>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                    <table class=""row row-4"" align=""center"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; background-color: #ffffff;"">
                        <tbody>
                            <tr>
                                <td>
                                    <table class=""row-content stack"" align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; color: #000000; width: 600px; margin: 0 auto;"" width=""600"">
                                        <tbody>
                                            <tr>
                                                <td class=""column column-1"" width=""100%"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; font-weight: 400; text-align: left; padding-bottom: 5px; padding-top: 5px; vertical-align: top;"">
                                                    <table class=""icons_block block-1"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; text-align: center; line-height: 0;"">
                                                        <tr>
                                                            <td class=""pad"" style=""vertical-align: middle; color: #1e0e4b; font-family: 'Inter', sans-serif; font-size: 15px; padding-bottom: 5px; padding-top: 5px; text-align: center;""><table class=""icons-inner"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; display: inline-block; padding-left: 0px; padding-right: 0px;"" cellpadding=""0"" cellspacing=""0"" role=""presentation""><tr>
                                                                    <td style=""vertical-align: middle; text-align: center; padding-top: 5px; padding-bottom: 5px; padding-left: 5px; padding-right: 6px;""><a href=""http://designedwithbeefree.com/"" target=""_blank"" style=""text-decoration: none;""><img class=""icon"" alt=""Beefree Logo"" src=""https://d1oco4z2z1fhwp.cloudfront.net/assets/Beefree-logo.png"" height=""auto"" width=""34"" align=""center"" style=""display: block; height: auto; margin: 0 auto; border: 0;""></a></td>
                                                                    <td style=""font-family: 'Inter', sans-serif; font-size: 15px; font-weight: undefined; color: #1e0e4b; vertical-align: middle; letter-spacing: undefined; text-align: center; line-height: normal;""><a href=""http://designedwithbeefree.com/"" target=""_blank"" style=""color: #1e0e4b; text-decoration: none;"">Designed with Beefree</a></td>
                                                                </tr>
                                                            </table>
                                                        </td>
                                                    </tr>
                                                </table>
                                            </td>
                                        </tr>
                                    </tbody>
                                </table>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </td>
        </tr>
    </tbody>
</table>
</body>

</html>
";

                await SendEmailAsync(recipientEmail, subject, message);
            }
        }

        public async Task SendBudgetPartPercentageEmail(string recipientEmail, AlertBudgetPartDTO budgetPartDto, decimal percentage)
        {
            var partName = budgetPartDto.PartName;
            var allocatedAmount = budgetPartDto.AllocatedAmount;
            var remainingAmount = budgetPartDto.RemainingAmount;
            var usedAmount = allocatedAmount - remainingAmount;
            var currentPercentage = (usedAmount / allocatedAmount) * 100;

            if (currentPercentage >= percentage)
            {
                var subject = $"Alerta: Partida Presupuestaria '{partName}' alcanzó el {currentPercentage:N2}% de su presupuesto";

                // Usamos la plantilla HTML para el correo electrónico
                var message = $@"
<!DOCTYPE html>
<html xmlns:v=""urn:schemas-microsoft-com:vml"" xmlns:o=""urn:schemas-microsoft-com:office:office"" lang=""en"">

<head>
	<title></title>
	<meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"">
	<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
	<style>
		* {{
			box-sizing: border-box;
		}}

		body {{
			margin: 0;
			padding: 0;
		}}

		a[x-apple-data-detectors] {{
			color: inherit !important;
			text-decoration: inherit !important;
		}}

		#MessageViewBody a {{
			color: inherit;
			text-decoration: none;
		}}

		p {{
			line-height: inherit
		}}

		.desktop_hide,
		.desktop_hide table {{
			mso-hide: all;
			display: none;
			max-height: 0px;
			overflow: hidden;
		}}

		.image_block img+div {{
			display: none;
		}}

		sup,
		sub {{
			font-size: 75%;
			line-height: 0;
		}}

		@media (max-width:620px) {{
			.desktop_hide table.icons-inner {{
				display: inline-block !important;
			}}

			.icons-inner {{
				text-align: center;
			}}

			.icons-inner td {{
				margin: 0 auto;
			}}

			.mobile_hide {{
				display: none;
			}}

			.row-content {{
				width: 100% !important;
			}}

			.stack .column {{
				width: 100%;
				display: block;
			}}

			.mobile_hide {{
				min-height: 0;
				max-height: 0;
				max-width: 0;
				overflow: hidden;
				font-size: 0px;
			}}

			.desktop_hide,
			.desktop_hide table {{
				display: table !important;
				max-height: none !important;
			}}
		}}
	</style>
</head>

<body class=""body"" style=""background-color: #ffffff; margin: 0; padding: 0; -webkit-text-size-adjust: none; text-size-adjust: none;"">
	<table class=""nl-container"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; background-color: #ffffff;"">
		<tbody>
			<tr>
				<td>
					<table class=""row row-1"" align=""center"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
						<tbody>
							<tr>
								<td>
									<table class=""row-content stack"" align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; color: #000000; width: 600px; margin: 0 auto;"" width=""600"">
										<tbody>
											<tr>
												<td class=""column column-1"" width=""100%"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; font-weight: 400; text-align: left; padding-bottom: 5px; padding-top: 5px; vertical-align: top;"">
													<table class=""heading_block block-1"" width=""100%"" border=""0"" cellpadding=""10"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
														<tr>
															<td class=""pad"">
																<h1 style=""margin: 0; color: #3a889d; direction: ltr; font-family: Arial, Helvetica, sans-serif; font-size: 38px; font-weight: 700; letter-spacing: normal; line-height: 1.2; text-align: left; margin-top: 0; margin-bottom: 0; mso-line-height-alt: 46px;""><span class=""tinyMce-placeholder"" style=""word-break: break-word;""><u><strong>¡AVISO MONETARIO!</strong></u></span></h1>
															</td>
														</tr>
													</table>
												</td>
											</tr>
										</tbody>
									</table>
								</td>
							</tr>
						</tbody>
					</table>
					<table class=""row row-2"" align=""center"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
						<tbody>
							<tr>
								<td>
									<table class=""row-content stack"" align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-radius: 0; color: #000000; width: 600px; margin: 0 auto;"" width=""600"">
										<tbody>
											<tr>
												<td class=""column column-1"" width=""100%"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; font-weight: 400; text-align: left; padding-bottom: 5px; padding-top: 5px; vertical-align: top;"">
													<table class=""heading_block block-1"" width=""100%"" border=""0"" cellpadding=""10"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
														<tr>
															<td class=""pad"">
																<h1 style=""margin: 0; color: #3a889d; direction: ltr; font-family: Arial, Helvetica, sans-serif; font-size: 38px; font-weight: 700; letter-spacing: normal; line-height: 1.2; text-align: left; margin-top: 0; margin-bottom: 0; mso-line-height-alt: 46px;""><span class=""tinyMce-placeholder"" style=""word-break: break-word;"">Estimado(a) Manager,</span></h1>
															</td>
														</tr>
													</table>
												</td>
											</tr>
										</tbody>
									</table>
								</td>
							</tr>
						</tbody>
					</table>
					<table class=""row row-3"" align=""center"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
						<tbody>
							<tr>
								<td>
									<table class=""row-content stack"" align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-radius: 0; color: #000000; width: 600px; margin: 0 auto;"" width=""600"">
										<tbody>
											<tr>
												<td class=""column column-1"" width=""100%"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; font-weight: 400; text-align: left; padding-bottom: 5px; padding-top: 5px; vertical-align: top;"">
													<table class=""paragraph_block block-1"" width=""100%"" border=""0"" cellpadding=""10"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; word-break: break-word;"">
														<tr>
															<td class=""pad"">
																<div style=""color:#101112;direction:ltr;font-family:Arial, Helvetica, sans-serif;font-size:16px;font-weight:400;letter-spacing:0px;line-height:1.2;text-align:left;mso-line-height-alt:19px;"">
																	<p style=""margin: 0;"">La partida presupuestaria <strong>{partName}</strong> ha alcanzado un <strong>{currentPercentage:N2}%</strong> de su presupuesto, por favor tome medidas preventivas. </p>
																	<p style=""margin: 0;""><strong>Presupuesto Asignado:</strong> ₡{allocatedAmount:N2}</p>
																	<p style=""margin: 0;""><strong>Presupuesto Usado:</strong> ₡{usedAmount:N2}</p>
																	<p style=""margin: 0;""><strong>Presupuesto Restante:</strong> ₡{remainingAmount:N2}</p>
																</div>
															</td>
														</tr>
													</table>
												</td>
											</tr>
										</tbody>
									</table>
								</td>
							</tr>
						</tbody>
					</table>
					<table class=""row row-4"" align=""center"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; background-color: #ffffff;"">
						<tbody>
							<tr>
								<td>
									<table class=""row-content stack"" align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; color: #000000; width: 600px; margin: 0 auto;"" width=""600"">
										<tbody>
											<tr>
												<td class=""column column-1"" width=""100%"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; font-weight: 400; text-align: left; padding-bottom: 5px; padding-top: 5px; vertical-align: top;"">
													<table class=""icons_block block-1"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; text-align: center; line-height: 0;"">
														<tr>
															<td class=""pad"" style=""vertical-align: middle; color: #1e0e4b; font-family: 'Inter', sans-serif; font-size: 15px; padding-bottom: 5px; padding-top: 5px; text-align: center;""><table class=""icons-inner"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; display: inline-block; padding-left: 0px; padding-right: 0px;"" cellpadding=""0"" cellspacing=""0"" role=""presentation""><tr>
																		<td style=""vertical-align: middle; text-align: center; padding-top: 5px; padding-bottom: 5px; padding-left: 5px; padding-right: 6px;""><a href=""http://designedwithbeefree.com/"" target=""_blank"" style=""text-decoration: none;""><img class=""icon"" alt=""Beefree Logo"" src=""https://d1oco4z2z1fhwp.cloudfront.net/assets/Beefree-logo.png"" height=""auto"" width=""34"" align=""center"" style=""display: block; height: auto; margin: 0 auto; border: 0;""></a></td>
																		<td style=""font-family: 'Inter', sans-serif; font-size: 15px; font-weight: undefined; color: #1e0e4b; vertical-align: middle; letter-spacing: undefined; text-align: center; line-height: normal;""><a href=""http://designedwithbeefree.com/"" target=""_blank"" style=""color: #1e0e4b; text-decoration: none;"">Designed with Beefree</a></td>
																	</tr>
																</table>
															</td>
														</tr>
													</table>
												</td>
											</tr>
										</tbody>
									</table>
								</td>
							</tr>
						</tbody>
					</table>
				</td>
			</tr>
		</tbody>
	</table>
</body>

</html>
";

                await SendEmailAsync(recipientEmail, subject, message);
            }
        }


    }
}
