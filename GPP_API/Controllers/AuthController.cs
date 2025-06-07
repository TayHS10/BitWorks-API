using GPP_API.DTO.User;
using GPP_API.Models;
using GPP_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GPP_API.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthorizationService _authorizationServices;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public AuthController(ApplicationDbContext context, IEmailService emailService, IConfiguration configuration, IAuthorizationService authorizationServices)
        {
            _context = context;
            _authorizationServices = authorizationServices;
            _emailService = emailService;
            _configuration = configuration;
        }

        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] LoginUserDTO authenticated)
        {
            var authorized = await _authorizationServices.ReturnToken(authenticated);

            if (authorized == null)
            {
                return Unauthorized(new { success = false, message = "Invalid credentials." });
            }

            return Ok(authorized);
        }

        [HttpPost("forgot-password-request")]
        public async Task<IActionResult> ForgotPasswordRequest([FromBody] ForgotPasswordRequestDTO requestData)
        {
            if (requestData == null || string.IsNullOrWhiteSpace(requestData.Email))
            {
                return BadRequest(new { success = false, message = "La dirección de correo electrónico es requerida." });
            }

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.Equals(requestData.Email));

                if (user == null)
                {
                    Console.WriteLine($"DEBUG: Intento de restablecer la contraseña para un correo no existente: {requestData.Email}");
                    return Ok(new { success = true, message = "Si existe un usuario con ese correo electrónico, se ha enviado un enlace para restablecer la contraseña." });
                }

                string resetToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
                DateTime expiryTime = DateTime.UtcNow.AddMinutes(30);

                var newResetToken = new PasswordResetToken
                {
                    UserId = user.UserId,
                    Token = resetToken,
                    ExpiresAt = expiryTime,
                    CreatedAt = DateTime.UtcNow
                };

                _context.PasswordResetTokens.Add(newResetToken);
                await _context.SaveChangesAsync();

                var frontendUrl = _configuration["FrontendUrl"];
                if (string.IsNullOrEmpty(frontendUrl))
                {
                    Console.WriteLine("Advertencia: La URL del frontend no está configurada en appsettings.json. No se pudo generar el enlace de restablecimiento de contraseña.");
                    return StatusCode(500, new { success = false, message = "Ocurrió un error al procesar la solicitud de restablecimiento de contraseña: URL del frontend no configurada." });
                }

                var resetLink = $"{frontendUrl}/reset-password?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(resetToken)}";
                var subject = "Restablecimiento de contraseña para tu cuenta";

                // --- INICIO DE LA PLANTILLA HTML ACTUALIZADA ---
                var messageBody = $@"
                    <!DOCTYPE html>
                    <html xmlns:v=""urn:schemas-microsoft-com:vml"" xmlns:o=""urn:schemas-microsoft-com:office:office"" lang=""en"">

                    <head>
                        <title>Restablecimiento de Contraseña</title>
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

                            @media (max-width:520px) {{
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

                    <body class=""body"" style=""background-color: #FFFFFF; margin: 0; padding: 0; -webkit-text-size-adjust: none; text-size-adjust: none;"">
                        <table class=""nl-container"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; background-color: #FFFFFF;"">
                            <tbody>
                                <tr>
                                    <td>
                                        <table class=""row row-1"" align=""center"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
                                            <tbody>
                                                <tr>
                                                    <td>
                                                        <table class=""row-content stack"" align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; color: #000000; width: 500px; margin: 0 auto;"" width=""500"">
                                                            <tbody>
                                                                <tr>
                                                                    <td class=""column column-1"" width=""100%"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; font-weight: 400; text-align: left; padding-bottom: 5px; padding-top: 5px; vertical-align: top;"">
                                                                        <table class=""heading_block block-1"" width=""100%"" border=""0"" cellpadding=""10"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
                                                                            <tr>
                                                                                <td class=""pad"">
                                                                                    <h1 style=""margin: 0; color: #1e0e4b; direction: ltr; font-family: Arial, 'Helvetica Neue', Helvetica, sans-serif; font-size: 38px; font-weight: 700; letter-spacing: normal; line-height: 1.2; text-align: left; margin-top: 0; margin-bottom: 0; mso-line-height-alt: 46px;""><span class=""tinyMce-placeholder"" style=""word-break: break-word;"">Restablecer contraseña</span></h1>
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
                                                        <table class=""row-content stack"" align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-radius: 0; color: #000000; width: 500px; margin: 0 auto;"" width=""500"">
                                                            <tbody>
                                                                <tr>
                                                                    <td class=""column column-1"" width=""100%"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; font-weight: 400; text-align: left; padding-bottom: 5px; padding-top: 5px; vertical-align: top;"">
                                                                        <table class=""paragraph_block block-1"" width=""100%"" border=""0"" cellpadding=""10"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; word-break: break-word;"">
                                                                            <tr>
                                                                                <td class=""pad"">
                                                                                    <div style=""color:#101112;direction:ltr;font-family:Arial, 'Helvetica Neue', Helvetica, sans-serif;font-size:16px;font-weight:400;letter-spacing:0px;line-height:1.2;text-align:left;mso-line-height-alt:19px;"">
                                                                                        <p style=""margin: 0;"">Hemos recibido una solicitud para <strong>restablecer la contraseña</strong> de tu cuenta. Haz clic en el siguiente enlace para establecer una nueva contraseña:</p>
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
                                                        <table class=""row-content stack"" align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-radius: 0; color: #000000; width: 500px; margin: 0 auto;"" width=""500"">
                                                            <tbody>
                                                                <tr>
                                                                    <td class=""column column-1"" width=""100%"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; font-weight: 400; text-align: left; padding-bottom: 5px; padding-top: 5px; vertical-align: top;"">
                                                                        <table class=""button_block block-1"" width=""100%"" border=""0"" cellpadding=""10"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
                                                                            <tr>
                                                                                <td class=""pad"">
                                                                                    <div class=""alignment"" align=""center""><a href='{resetLink}' target=""_blank"" style=""background-color: #7747FF; border-bottom: 0px solid transparent; border-left: 0px solid transparent; border-radius: 4px; border-right: 0px solid transparent; border-top: 0px solid transparent; color: #ffffff; display: inline-block; font-family: Arial, 'Helvetica Neue', Helvetica, sans-serif; font-size: 16px; font-weight: 400; mso-border-alt: none; padding-bottom: 5px; padding-top: 5px; padding-left: 20px; padding-right: 20px; text-align: center; width: auto; word-break: keep-all; line-height: 32px; text-decoration: none;""><span style=""word-break: break-word;"">Restablecer</span></a></div>
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
                                                        <table class=""row-content stack"" align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-radius: 0; color: #000000; width: 500px; margin: 0 auto;"" width=""500"">
                                                            <tbody>
                                                                <tr>
                                                                    <td class=""column column-1"" width=""100%"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; font-weight: 400; text-align: left; padding-bottom: 5px; padding-top: 5px; vertical-align: top;"">
                                                                        <table class=""paragraph_block block-1"" width=""100%"" border=""0"" cellpadding=""10"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; word-break: break-word;"">
                                                                            <tr>
                                                                                <td class=""pad"">
                                                                                    <div style=""color:#101112;direction:ltr;font-family:Arial, 'Helvetica Neue', Helvetica, sans-serif;font-size:16px;font-weight:400;letter-spacing:0px;line-height:1.2;text-align:left;mso-line-height-alt:19px;"">
                                                                                        <p style=""margin: 0;"">Este enlace expirará en <u><strong>30 minutos</strong></u>. Si no solicitaste un restablecimiento de contraseña, ignora este correo electrónico.</p>
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
                                        <table class=""row row-5"" align=""center"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
                                            <tbody>
                                                <tr>
                                                    <td>
                                                        <table class=""row-content stack"" align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-radius: 0; color: #000000; width: 500px; margin: 0 auto;"" width=""500"">
                                                            <tbody>
                                                                <tr>
                                                                    <td class=""column column-1"" width=""100%"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; font-weight: 400; text-align: left; padding-bottom: 5px; padding-top: 5px; vertical-align: top;"">
                                                                        <table class=""divider_block block-1"" width=""100%"" border=""0"" cellpadding=""10"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
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
                                        <table class=""row row-6"" align=""center"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
                                            <tbody>
                                                <tr>
                                                    <td>
                                                        <table class=""row-content stack"" align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-radius: 0; color: #000000; width: 500px; margin: 0 auto;"" width=""500"">
                                                            <tbody>
                                                                <tr>
                                                                    <td class=""column column-1"" width=""100%"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; font-weight: 400; text-align: left; padding-bottom: 5px; padding-top: 5px; vertical-align: top;"">
                                                                        <table class=""paragraph_block block-1"" width=""100%"" border=""0"" cellpadding=""10"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; word-break: break-word;"">
                                                                            <tr>
                                                                                <td class=""pad"">
                                                                                    <div style=""color:#101112;direction:ltr;font-family:Arial, 'Helvetica Neue', Helvetica, sans-serif;font-size:16px;font-weight:400;letter-spacing:0px;line-height:1.2;text-align:left;mso-line-height-alt:19px;"">
                                                                                        <p style=""margin: 0;"">Saludos, Equipo de GPP</p>
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
                                                        <table class=""row-content stack"" align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; color: #000000; background-color: #ffffff; width: 500px; margin: 0 auto;"" width=""500"">
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
                // --- FIN DE LA PLANTILLA HTML ACTUALIZADA ---

                try
                {
                    await _emailService.SendEmailAsync(user.Email, subject, messageBody);
                    Console.WriteLine($"DEBUG: Token de restablecimiento de contraseña para {user.Email}: {resetToken}. Expira a las {expiryTime}. Correo electrónico enviado.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al enviar el correo electrónico: {ex.Message}");
                    return StatusCode(500, new { success = false, message = "Ocurrió un error al enviar el correo electrónico de restablecimiento de contraseña.", detail = ex.Message });
                }

                return Ok(new { success = true, message = "Si existe un usuario con ese correo electrónico, se ha enviado un enlace para restablecer la contraseña." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al procesar la solicitud de restablecimiento de contraseña: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Ocurrió un error al procesar la solicitud de restablecimiento de contraseña.", detail = ex.Message });
            }
        }

        [HttpPost("reset-password-confirm")]
        public async Task<IActionResult> ConfirmPasswordReset([FromBody] ResetPasswordConfirmDTO resetData)
        {
            if (resetData == null ||
                string.IsNullOrWhiteSpace(resetData.Email) ||
                string.IsNullOrWhiteSpace(resetData.Token) ||
                string.IsNullOrWhiteSpace(resetData.NewPassword) ||
                string.IsNullOrWhiteSpace(resetData.ConfirmNewPassword))
            {
                return BadRequest(new { success = false, message = "El correo electrónico, el token, la nueva contraseña y la confirmación son requeridos." });
            }

            if (resetData.NewPassword != resetData.ConfirmNewPassword)
            {
                return BadRequest(new { success = false, message = "La nueva contraseña y la contraseña de confirmación no coinciden." });
            }

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.Equals(resetData.Email));

                if (user == null)
                {
                    return NotFound(new { success = false, message = "No se encontró ningún usuario con la dirección de correo electrónico proporcionada." });
                }

                var validToken = await _context.PasswordResetTokens
                    .Where(t => t.UserId == user.UserId &&
                                 t.Token == resetData.Token &&
                                 t.ExpiresAt > DateTime.UtcNow &&
                                 t.UsedAt == null)
                    .OrderByDescending(t => t.CreatedAt)
                    .FirstOrDefaultAsync();

                if (validToken == null)
                {
                    return Unauthorized(new { success = false, message = "Token de restablecimiento de contraseña inválido o expirado." });
                }

                user.Password = BCrypt.Net.BCrypt.HashPassword(resetData.NewPassword);
                user.Status = "Active";

                validToken.UsedAt = DateTime.UtcNow;

                _context.Users.Update(user);
                _context.PasswordResetTokens.Update(validToken);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "La contraseña se ha restablecido correctamente. Ahora puedes iniciar sesión con tu nueva contraseña." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error durante el restablecimiento de contraseña para {resetData.Email}: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Ocurrió un error durante el restablecimiento de contraseña.", detail = ex.Message });
            }
        }
    }
}