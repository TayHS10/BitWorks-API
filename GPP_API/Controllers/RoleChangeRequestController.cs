// Importación de bibliotecas necesarias
using BCrypt.Net; // Asegúrate de tener este paquete NuGet instalado
using GPP_API.Controllers;
using GPP_API.DTO.Alert;
using GPP_API.DTO.BudgetPart;
using GPP_API.DTO.Expense;
using GPP_API.DTO.Project;
using GPP_API.DTO.RoleChangeRequest; // Asegúrate de que esta sea la ruta correcta a tu DTO
using GPP_API.DTO.User;
using GPP_API.Models;
using GPP_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;

// Usados para la generación de contraseñas
using System.Security.Cryptography;
using System.Text;

namespace GPP_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoleChangeRequestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<RoleChangeRequestController> _logger;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="RoleChangeRequestController"/>.
        /// </summary>
        /// <param name="context">El contexto de la base de datos para acceder a los datos de la aplicación.</param>
        /// <param name="emailService">El servicio de correo electrónico para enviar notificaciones (opcional).</param>
        /// <param name="logger">El servicio de registro para registrar información y errores.</param>
        public RoleChangeRequestController(ApplicationDbContext context, IEmailService emailService, ILogger<RoleChangeRequestController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Recupera todas las solicitudes de cambio de rol que están en estado "Activo" o "Pendiente".
        /// He ajustado el filtro para incluir "Pending" ya que las nuevas solicitudes tendrán este estado.
        /// </summary>
        /// <returns>Una lista de solicitudes de cambio de rol activas/pendientes en formato DTO o un mensaje de error en caso de fallo.</returns>
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveRoleChangeRequests()
        {
            try
            {
                var activeRequests = await _context.RoleChangeRequests
                    .Where(r => r.Status == "Active" || r.Status == "Pending") 
                    .ToListAsync();

                var dtoList = activeRequests.Select(MapToRoleChangeRequestDTO).ToList();

                return Ok(new { success = true, data = dtoList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocurrió un error al recuperar las solicitudes de cambio de rol activas.");
                return StatusCode(500, new { success = false, message = "Error al obtener solicitudes de cambio de rol activas.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Recupera una solicitud de cambio de rol específica por su ID.
        /// </summary>
        /// <param name="id">El ID de la solicitud de cambio de rol a recuperar.</param>
        /// <returns>La solicitud en formato DTO si se encuentra, o un mensaje de error si no se encuentra.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoleChangeRequest(int id)
        {
            try
            {
                var request = await _context.RoleChangeRequests
                    .FirstOrDefaultAsync(r => r.RequestId == id);

                if (request == null)
                {
                    return NotFound(new { success = false, message = $"Solicitud de cambio de rol con ID {id} no encontrada." });
                }

                var dto = MapToRoleChangeRequestDTO(request);

                return Ok(new { success = true, data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocurrió un error al recuperar la solicitud de cambio de rol con ID {RequestId}.", id);
                return StatusCode(500, new { success = false, message = "Error al obtener la solicitud de cambio de rol.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Crea una nueva solicitud de cambio de rol. Requiere que se proporcionen el EmailAddress y FullName.
        /// Si el usuario no existe, solo se crea el usuario. Si el usuario existe, se procesa la solicitud de cambio de rol.
        /// </summary>
        /// <param name="createDto">Los datos necesarios para crear la solicitud de cambio de rol.</param>
        /// <returns>Resultado de la acción que indica el éxito de la creación, o un mensaje de error en caso de fallo.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateRoleChangeRequest(CreateRoleChangeRequestDTO createDto)
        {
            if (createDto == null)
            {
                _logger.LogWarning("CreateRoleChangeRequest: Los datos de la solicitud de cambio de rol están incompletos");
                return BadRequest(new { success = false, message = "Los datos de la solicitud de cambio de rol no pueden estar vacíos." });
            }

            try
            {

                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == createDto.EmailAddress);
                string generatedPassword = null;

                if (existingUser == null)
                {
                    if (!IsValidEmail(createDto.EmailAddress))
                    {
                        _logger.LogWarning("CreateRoleChangeRequest: El formato del correo electrónico '{Email}' no es válido para la creación de un nuevo usuario.", createDto.EmailAddress);
                        return BadRequest(new { success = false, message = "El formato del correo electrónico proporcionado no es válido." });
                    }
                    _logger.LogInformation("CreateRoleChangeRequest: Usuario con email {Email} no encontrado. Procediendo a crear un nuevo usuario.", createDto.EmailAddress);

                    generatedPassword = GenerateRandomPassword(8);
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(generatedPassword);

                    var newUser = new User
                    {
                        FullName = createDto.FullName,
                        Email = createDto.EmailAddress,
                        Role = createDto.RequestedRole,
                        Password = hashedPassword,
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Users.Add(newUser);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("CreateRoleChangeRequest: Nuevo usuario (Email: {Email}) creado exitosamente con rol inicial '{Role}'.", createDto.EmailAddress, createDto.RequestedRole);

                    if (!string.IsNullOrEmpty(newUser.Email) && _emailService != null)
                    {
                        try
                        {
                            await _emailService.SendNewUserWelcomeEmail(newUser.Email, newUser.FullName, generatedPassword, newUser.Role);
                            _logger.LogInformation("CreateRoleChangeRequest: Correo electrónico de bienvenida con detalles de cuenta enviado a {Email}.", newUser.Email);
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogError(emailEx, "CreateRoleChangeRequest: Error al enviar el correo electrónico de bienvenida para el nuevo usuario {Email}.", newUser.Email);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("CreateRoleChangeRequest: No se pudo enviar correo electrónico de bienvenida para {Email}: el servicio de correo no está disponible o el email está vacío.", newUser.Email);
                    }

                    return Ok(new { success = true, message = "Usuario creado exitosamente. Se ha enviado un correo de bienvenida con los detalles de acceso." });
                }
                else
                {
                    _logger.LogInformation("CreateRoleChangeRequest: Usuario con email {Email} ya existe. Procesando solicitud de cambio de rol.", createDto.EmailAddress);

                    if (existingUser.Status.ToLower() != "active")
                    {
                        _logger.LogWarning("CreateRoleChangeRequest: El usuario existente {Email} no está activo y no puede solicitar un cambio de rol.", createDto.EmailAddress);
                        return BadRequest(new { success = false, message = "El usuario existente no está activo y no puede solicitar un cambio de rol." });
                    }

                    var request = new RoleChangeRequest
                    {
                        EmailAddress = createDto.EmailAddress,
                        FullName = createDto.FullName,
                        Justification = createDto.Justification,
                        RequestedRole = createDto.RequestedRole,
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.RoleChangeRequests.Add(request);
                    await _context.SaveChangesAsync();

                    var resultDto = MapToRoleChangeRequestDTO(request);

                    _logger.LogInformation($"Nueva solicitud de cambio de rol (ID: {resultDto.RequestId}) creada para el email: {resultDto.EmailAddress}.");

                    return CreatedAtAction(nameof(GetRoleChangeRequest), new { id = request.RequestId },
                        new { success = true, message = "Solicitud de cambio de rol creada exitosamente. Pendiente de aprobación.", data = resultDto });
                }
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Ocurrió un error de base de datos al procesar la solicitud de cambio de rol o la creación de usuario.");
                return StatusCode(500, new { success = false, message = "Error al procesar la solicitud. Contacte a soporte.", detail = dbEx.InnerException?.Message ?? dbEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocurrió un error inesperado al procesar la solicitud de cambio de rol o la creación de usuario.");
                return StatusCode(500, new { success = false, message = "Ocurrió un error inesperado. Contacte a soporte.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Verifica de forma breve si una cadena tiene el formato de un correo electrónico válido.
        /// </summary>
        /// <param name="email">La cadena a verificar.</param>
        /// <returns>True si el formato es válido, false en caso contrario.</returns>
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Actualiza una solicitud de cambio de rol específica con los nuevos datos proporcionados.
        /// Solo permite actualizar ciertos campos si la solicitud está pendiente.
        /// </summary>
        /// <param name="id">El ID de la solicitud de cambio de rol a actualizar.</param>
        /// <param name="updateDto">El DTO que contiene los nuevos datos de la solicitud.</param>
        /// <returns>Un mensaje de éxito si la actualización es exitosa, o un mensaje de error si la solicitud no se encuentra o ocurre un fallo.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRoleChangeRequest(int id, UpdateRoleChangeRequestDTO updateDto)
        {
            try
            {
                var request = await _context.RoleChangeRequests.FindAsync(id);

                if (request == null)
                    return NotFound(new { success = false, message = $"Solicitud de cambio de rol con ID {id} no encontrada." });

               
                if (request.Status == "Pending")
                {
                    request.RequestedRole = updateDto.RequestedRole ?? request.RequestedRole;
                    request.Justification = updateDto.Justification ?? request.Justification;
                    request.EmailAddress = updateDto.EmailAddress ?? request.EmailAddress;
                    request.FullName = updateDto.FullName ?? request.FullName;
                }
                else
                {
                    
                    return BadRequest(new { success = false, message = $"La solicitud no puede ser actualizada porque su estado es '{request.Status}'. Solo las solicitudes 'Pending' pueden ser modificadas." });
                }

                await _context.SaveChangesAsync();

                var updatedDto = MapToRoleChangeRequestDTO(request);

                return Ok(new { success = true, message = "Solicitud de cambio de rol actualizada exitosamente.", data = updatedDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocurrió un error al actualizar la solicitud de cambio de rol con ID {RequestId}.", id);
                return StatusCode(500, new { success = false, message = "Error al actualizar la solicitud de cambio de rol.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Desactiva una solicitud de cambio de rol específica cambiando su estado a "Cancelled" y envía una notificación por correo electrónico.
        /// </summary>
        /// <param name="id">El ID de la solicitud de cambio de rol a desactivar.</param>
        /// <returns>Un mensaje de éxito si la desactivación es exitosa, o un mensaje de error si la solicitud no se encuentra, ya está inactiva, o ocurre un fallo.</returns>
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelRoleChangeRequest(int id)
        {
            try
            {
                var request = await _context.RoleChangeRequests.FindAsync(id);

                if (request == null)
                {
                    _logger.LogWarning("CancelRoleChangeRequest: Solicitud con ID {Id} no encontrada.", id);
                    return NotFound(new { success = false, message = $"Solicitud de cambio de rol con ID {id} no encontrada." });
                }

                if (request.Status == "Rejected")
                {
                    _logger.LogWarning("CancelRoleChangeRequest: Solicitud con ID {Id} ya está cancelada.", id);
                    return BadRequest(new { success = false, message = $"La solicitud con ID {id} ya se encuentra cancelada." });
                }

                if (request.Status != "Pending")
                {
                    _logger.LogWarning("CancelRoleChangeRequest: Solicitud con ID {Id} tiene estado '{Status}', no puede ser cancelada desde este estado.", id, request.Status);
                    return BadRequest(new { success = false, message = $"La solicitud no está en estado 'Pending' y no puede ser cancelada. Estado actual: '{request.Status}'." });
                }

                request.Status = "Rejected"; 
                await _context.SaveChangesAsync();

                var requestDto = MapToRoleChangeRequestDTO(request); 

                try
                {
                    if (!string.IsNullOrEmpty(requestDto.EmailAddress))
                    {
                        await _emailService.SendRoleChangeRequestRejectedEmail(requestDto.EmailAddress, requestDto);
                        _logger.LogInformation($"Correo de rechazo de solicitud de rol enviado exitosamente para la solicitud {requestDto.RequestId} al email {requestDto.EmailAddress}.");
                        return Ok(new { success = true, message = $"Solicitud de cambio de rol (ID: {request.RequestId}) cancelada exitosamente y correo de notificación enviado." });
                    }
                    else
                    {
                        _logger.LogWarning($"No se pudo enviar correo de rechazo para solicitud {request.RequestId}: EmailAddress es nulo o vacío.");
                        return Ok(new { success = true, message = $"Solicitud de cambio de rol (ID: {request.RequestId}) cancelada exitosamente, pero no se pudo enviar el correo de notificación (email no disponible)." });
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, $"Error al enviar el correo de rechazo para la solicitud de cambio de rol {request.RequestId}.");
                   
                    return Ok(new { success = true, message = "Solicitud de cambio de rol cancelada exitosamente, pero hubo un error al enviar el correo de notificación.", emailError = emailEx.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CancelRoleChangeRequest: Ocurrió un error al cancelar la solicitud de cambio de rol con ID {RequestId}.", id);
                return StatusCode(500, new { success = false, message = "Error al cancelar la solicitud de cambio de rol.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Aprueba una solicitud de cambio de rol. Si la solicitud es para un nuevo usuario, lo crea.
        /// Si es para un usuario existente, podría actualizar su rol (lógica que no se implementa aquí, solo la creación).
        /// </summary>
        /// <param name="id">El ID de la solicitud de cambio de rol a aprobar.</param>
        /// <returns>Un mensaje de éxito si la aprobación y creación/actualización del usuario es exitosa, o un mensaje de error en caso de fallo.</returns>
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> ApproveRoleChangeRequest(int id)
        {
            try
            {
                var request = await _context.RoleChangeRequests.FindAsync(id);

                if (request == null)
                {
                    _logger.LogWarning("ApproveRoleChangeRequest: Solicitud con ID {Id} no encontrada.", id);
                    return NotFound(new { success = false, message = $"Solicitud de cambio de rol con ID {id} no encontrada." });
                }

                if (request.Status != "Pending")
                {
                    _logger.LogWarning("ApproveRoleChangeRequest: Solicitud con ID {Id} no está en estado 'Pending'. Estado actual: '{Status}'.", id, request.Status);
                    return BadRequest(new { success = false, message = $"La solicitud con ID {id} no está en estado 'Pending' y no puede ser aprobada. Estado actual: '{request.Status}'." });
                }

                if (string.IsNullOrWhiteSpace(request.EmailAddress) || string.IsNullOrWhiteSpace(request.FullName))
                {
                    return BadRequest(new { success = false, message = "La solicitud de cambio de rol no contiene suficiente información (Email o FullName) para crear un usuario." });
                }

                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.EmailAddress);

                if (existingUser != null)
                {
                   
                    if (existingUser.Status.ToLower() == "active")
                    {
                        
                        _logger.LogInformation("ApproveRoleChangeRequest: El usuario con email '{Email}' ya existe y está activo. Actualizando su rol.", request.EmailAddress);
                        existingUser.Role = request.RequestedRole; 
                        _context.Users.Update(existingUser);
                        request.Status = "Approved"; 
                        await _context.SaveChangesAsync();

                        try
                        {
                            await _emailService.SendRoleUpdatedEmail(existingUser.Email, existingUser.Role);
                            _logger.LogInformation($"Correo de actualización de rol enviado para {existingUser.Email}.");
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogError(emailEx, $"Error al enviar correo de actualización de rol para {existingUser.Email}.");
                        }

                        return Ok(new { success = true, message = $"Solicitud de cambio de rol aprobada. El rol del usuario '{existingUser.Email}' ha sido actualizado a '{request.RequestedRole}'.", data = MapToRoleChangeRequestDTO(request) });
                    }
                    else
                    {
                        
                        _logger.LogWarning("ApproveRoleChangeRequest: El usuario con email '{Email}' ya existe pero está inactivo. La solicitud no puede ser aprobada para crear/activar.", request.EmailAddress);
                        request.Status = "Rejected";
                        await _context.SaveChangesAsync();
                        return Conflict(new { success = false, message = $"Un usuario con el email '{request.EmailAddress}' ya existe pero está inactivo. No se puede crear un nuevo usuario con este email." });
                    }
                }
                else
                {
                    string generatedPassword = GenerateRandomPassword(10);
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(generatedPassword);

                    var newUser = new User
                    {
                        FullName = request.FullName,
                        Email = request.EmailAddress,
                        Password = hashedPassword,
                        Role = request.RequestedRole,
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Users.Add(newUser);
                    request.Status = "Approved"; 
                    await _context.SaveChangesAsync();

                    try
                    {
                        await _emailService.SendNewUserWelcomeEmail(newUser.Email, newUser.FullName, generatedPassword, newUser.Role);
                        _logger.LogInformation($"Correo de bienvenida y credenciales enviados exitosamente para el nuevo usuario '{newUser.Email}'.");
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, $"Error al enviar el correo de bienvenida para el nuevo usuario '{newUser.Email}'.");
                        
                    }

                    return Ok(new { success = true, message = $"Solicitud de cambio de rol aprobada. Nuevo usuario '{newUser.Email}' creado con el rol '{newUser.Role}'. La contraseña se ha enviado por correo electrónico.", data = MapToRoleChangeRequestDTO(request) });
                }
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "ApproveRoleChangeRequest: Error de base de datos al aprobar la solicitud ID {Id}. Inner Exception: {InnerExceptionMessage}",
                                     id, dbEx.InnerException?.Message);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al aprobar la solicitud de cambio de rol y procesar el usuario en la base de datos.",
                    detail = dbEx.Message,
                    innerError = dbEx.InnerException?.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ApproveRoleChangeRequest: Error inesperado al aprobar la solicitud ID {Id}.", id);
                return StatusCode(500, new { success = false, message = "Error inesperado al aprobar la solicitud de cambio de rol.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Mapea una entidad de solicitud de cambio de rol a un objeto DTO de respuesta.
        /// </summary>
        /// <param name="r">La entidad de solicitud de cambio de rol a mapear.</param>
        /// <returns>Un objeto <see cref="RoleChangeRequestDTO"/> que representa la solicitud.</returns>
        private static RoleChangeRequestDTO MapToRoleChangeRequestDTO(RoleChangeRequest r) => new()
        {
            RequestId = r.RequestId,
            RequestedRole = r.RequestedRole,
            Justification = r.Justification,
            EmailAddress = r.EmailAddress, 
            FullName = r.FullName,     
            Status = r.Status,
            CreatedAt = r.CreatedAt
        };

        /// <summary>
        /// Genera una contraseña aleatoria de la longitud especificada.
        /// Utiliza caracteres alfanuméricos y especiales.
        /// </summary>
        /// <param name="length">La longitud deseada de la contraseña.</param>
        /// <returns>La contraseña generada.</returns>
        private string GenerateRandomPassword(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()";
            StringBuilder password = new StringBuilder();
            using (var rng = RandomNumberGenerator.Create()) // Usa RNG criptográficamente seguro
            {
                byte[] data = new byte[length];
                rng.GetBytes(data); // Llena el array con bytes aleatorios
                for (int i = 0; i < length; i++)
                {
                    // Mapea el byte aleatorio a un carácter de la cadena 'chars'
                    password.Append(chars[data[i] % chars.Length]);
                }
            }
            return password.ToString();
        }
    }
}