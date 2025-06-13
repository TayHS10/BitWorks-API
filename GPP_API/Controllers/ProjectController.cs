// Importación de bibliotecas necesarias
using GPP_API.DTO.Alert;
using GPP_API.DTO.BudgetPart;
using GPP_API.Controllers;
using GPP_API.DTO.Expense;
using GPP_API.DTO.Project;
using GPP_API.DTO.User;
using GPP_API.Models;
using GPP_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GPP_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<ProjectController> _logger;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ProjectController"/>.
        /// </summary>
        /// <param name="context">El contexto de la base de datos para acceder a los datos de la aplicación.</param>
        /// <param name="emailService">El servicio de correo electrónico para enviar notificaciones.</param>
        /// <param name="logger">El servicio de registro para registrar información y errores.</param>
        public ProjectController(ApplicationDbContext context, IEmailService emailService, ILogger<ProjectController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Recupera todos los proyectos que están en estado "Activo", incluyendo sus alertas y partidas presupuestarias.
        /// </summary>
        /// <returns>Una lista de proyectos activos en formato DTO o un mensaje de error en caso de fallo.</returns>
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveProjects()
        {
            try
            {
                var activeProjects = await _context.Projects
                    .Where(p => p.Status == "Active")
                    .Include(p => p.Alerts)
                    .Include(p => p.BudgetParts).ThenInclude(b => b.Expenses)
                    .Include(p => p.ManagerEmailNavigation)
                    .ToListAsync(); 

                var dtoList = activeProjects.Select(MapToProjectResponseDTO).ToList();

                return Ok(new { success = true, data = dtoList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocurrió un error al recuperar proyectos activos.");
                return StatusCode(500, new { success = false, message = "Error al obtener proyectos activos.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Recupera un proyecto específico por su ID, asegurando que esté activo y cargando todas las entidades relacionadas.
        /// </summary>
        /// <param name="id">El ID del proyecto a recuperar.</param>
        /// <returns>El proyecto en formato DTO si se encuentra activo, o un mensaje de error si no se encuentra o no está activo.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProject(int id)
        {
            try
            {
                var project = await _context.Projects
                    .Include(p => p.Alerts)
                    .Include(p => p.BudgetParts).ThenInclude(b => b.Expenses)
                    .Include(p => p.ManagerEmailNavigation)
                    .FirstOrDefaultAsync(p => p.ProjectId == id && p.Status == "Active");

                if (project == null)
                {
                    return NotFound(new { success = false, message = $"Proyecto con ID {id} y estado 'Activo' no encontrado." });
                }

                var dto = MapToProjectResponseDTO(project);

                return Ok(new { success = true, data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocurrió un error al recuperar el proyecto con ID {ProjectId}.", id);
                return StatusCode(500, new { success = false, message = "Error al obtener el proyecto.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Crea un nuevo proyecto con sus partes de presupuesto asociadas, validando que el gerente exista y que el presupuesto sea válido.
        /// </summary>
        /// <param name="dto">Los datos necesarios para crear el proyecto, incluyendo el código, nombre, descripción, presupuesto y partes del presupuesto.</param>
        /// <returns>Resultado de la acción que indica el éxito de la creación del proyecto, o un mensaje de error en caso de fallo.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateProject(CreateProjectDTO dto)
        {
            try
            {
                var managerExists = await _context.Users.AnyAsync(u => u.Email == dto.ManagerEmail);
                if (!managerExists)
                    return BadRequest(new { success = false, message = "El gerente especificado no existe." });

                if (dto.Budget <= 0)
                {
                    return BadRequest(new { success = false, message = "El presupuesto del proyecto debe ser mayor que cero." });
                }

                if (dto.BudgetParts == null || !dto.BudgetParts.Any())
                    return BadRequest(new { success = false, message = "Debe incluir al menos una parte del presupuesto." });

                if (!VerifyBudgetPartsTotal(dto))
                {
                    return BadRequest(new { success = false, message = "La suma de los montos de las partes del presupuesto no coincide con el presupuesto total del proyecto." });
                }

                var project = new Project
                {
                    ProjectCode = dto.ProjectCode,
                    ProjectName = dto.ProjectName,
                    Description = dto.Description,
                    Budget = dto.Budget,
                    RemainingBudget = dto.Budget,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    ManagerEmail = dto.ManagerEmail,
                    BudgetParts = dto.BudgetParts.Select(bp => new BudgetPart
                    {
                        PartName = bp.PartName,
                        AllocatedAmount = bp.AllocatedAmount,
                        RemainingAmount = bp.AllocatedAmount,
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow
                    }).ToList()
                };

                _context.Projects.Add(project);

                await _context.SaveChangesAsync();

                var resultDto = MapToProjectResponseDTO(project);

                try
                {
                    await _emailService.SendProjectCreatedEmail(resultDto.ManagerEmail, resultDto);

                    _logger.LogInformation($"Correo electrónico de confirmación enviado exitosamente para el proyecto {resultDto.ProjectCode} al gerente {resultDto.ManagerEmail}.");

                    return CreatedAtAction(nameof(GetProject), new { id = project.ProjectId },
                        new { success = true, message = "Proyecto creado y correo electrónico de confirmación enviado exitosamente.", data = resultDto });
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, $"Error al enviar el correo electrónico de confirmación para el proyecto {resultDto.ProjectCode}.");

                    return CreatedAtAction(nameof(GetProject), new { id = project.ProjectId },
                        new { success = true, message = "Proyecto creado exitosamente, pero hubo un error al enviar el correo electrónico de confirmación.", emailError = emailEx.Message, data = resultDto });
                }

            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Ocurrió un error de base de datos al crear el proyecto.");
                return StatusCode(500, new { success = false, message = "Error al guardar el proyecto en la base de datos.", detail = dbEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocurrió un error inesperado al crear el proyecto.");
                return StatusCode(500, new { success = false, message = "Ocurrió un error inesperado al crear el proyecto.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Verifica si la suma de los montos asignados a las partes del presupuesto coincide con el presupuesto total del proyecto.
        /// </summary>
        /// <param name="dto">El DTO que contiene las partes del presupuesto del proyecto.</param>
        /// <returns>True si la suma de los montos asignados es igual al presupuesto total, de lo contrario, false.</returns>
        private bool VerifyBudgetPartsTotal(CreateProjectDTO dto)
        {
            decimal sumOfAllocatedAmounts = dto.BudgetParts.Sum(bp => bp.AllocatedAmount);

            return sumOfAllocatedAmounts == dto.Budget;
        }

        /// <summary>
        /// Desactiva un proyecto específico cambiando su estado a "Inactivo" en lugar de eliminarlo físicamente.
        /// </summary>
        /// <param name="id">El ID del proyecto a desactivar.</param>
        /// <returns>Un mensaje de éxito si la desactivación es exitosa, o un mensaje de error si el proyecto no se encuentra o ocurre un fallo.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            try
            {
                var project = await _context.Projects.FindAsync(id);

                if (project == null)
                    return NotFound(new { success = false, message = $"Project with ID {id} not found." });

                project.Status = "Inactive";

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Project deactivated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error deactivating the project.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Actualiza un proyecto específico con los nuevos datos proporcionados, permitiendo la modificación de campos seleccionados.
        /// </summary>
        /// <param name="id">El ID del proyecto a actualizar.</param>
        /// <param name="dto">El DTO que contiene los nuevos datos del proyecto.</param>
        /// <returns>Un mensaje de éxito si la actualización es exitosa, o un mensaje de error si el proyecto no se encuentra o ocurre un fallo.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProject(int id, UpdateProjectDTO dto)
        {
            try
            {
                
                var project = await _context.Projects.FindAsync(id);

                if (project == null)
                    return NotFound(new { success = false, message = $"Project with ID {id} not found." });

                if (dto.ManagerEmail != null)
                {
                    var managerExists = await _context.Users.AnyAsync(u => u.Email == dto.ManagerEmail);
                    if (!managerExists)
                        return BadRequest(new { success = false, message = "The specified manager does not exist." });

                    project.ManagerEmail = dto.ManagerEmail;
                }

                project.ProjectCode = dto.ProjectCode ?? project.ProjectCode;
                project.ProjectName = dto.ProjectName ?? project.ProjectName;
                project.Description = dto.Description ?? project.Description;

                await _context.SaveChangesAsync();

                var updatedDto = MapToProjectResponseDTO(project);

                return Ok(new { success = true, message = "Project updated successfully.", data = updatedDto });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error updating the project.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Mapea una entidad de proyecto a un objeto DTO de respuesta de proyecto, incluyendo sus propiedades y entidades relacionadas.
        /// </summary>
        /// <param name="p">La entidad de proyecto a mapear.</param>
        /// <returns>Un objeto <see cref="ProjectResponseDTO"/> que representa el proyecto y sus datos asociados.</returns>
        private static ProjectResponseDTO MapToProjectResponseDTO(Project p) => new()
        {
            ProjectId = p.ProjectId,
            ProjectCode = p.ProjectCode,
            ProjectName = p.ProjectName,
            Description = p.Description,
            Budget = p.Budget,
            RemainingBudget = p.RemainingBudget,
            CreatedAt = p.CreatedAt,
            ManagerEmail = p.ManagerEmail,
            Status = p.Status,

            Manager = p.ManagerEmailNavigation == null ? null : new UserResponseDTO
            {
                UserId = p.ManagerEmailNavigation.UserId,
                FullName = p.ManagerEmailNavigation.FullName,
                Email = p.ManagerEmailNavigation.Email,
                Role = p.ManagerEmailNavigation.Role
            },

            Alerts = p.Alerts.Select(a => new AlertDTO
            {
                AlertId = a.AlertId,
                AlertType = a.AlertType,
                Message = a.Message,
                AlertDate = a.AlertDate
            }).ToList(),

            BudgetParts = p.BudgetParts.Select(b => new BudgetPartResponseDTO
            {
                BudgetPartId = b.BudgetPartId,
                PartName = b.PartName,
                AllocatedAmount = b.AllocatedAmount,
                RemainingAmount = b.RemainingAmount,
                CreatedAt = b.CreatedAt,

                Expenses = b.Expenses.Select(e => new ExpenseResponseDTO
                {
                    ExpenseId = e.ExpenseId,
                    ExpenseAmount = e.ExpenseAmount,
                    ExpenseDate = e.ExpenseDate,
                    DocumentReference = e.DocumentReference,
                    Description = e.Description,
                    Status = e.Status,
                    CreatedAt = e.CreatedAt
                }).ToList()
            }).ToList()
        };

        /// <summary>
        /// Desactiva un proyecto específico y todas sus partidas presupuestarias y gastos asociados, cambiando su estado a "Inactivo".
        /// </summary>
        /// <param name="id">El ID del proyecto a desactivar.</param>
        /// <returns>Un mensaje de éxito si la desactivación es exitosa, o un mensaje de error si el proyecto no se encuentra, ya está inactivo, o ocurre un fallo.</returns>
        [HttpPut("{id}/Deactivate")]
        public async Task<IActionResult> DeactivateProject(int id)
        {
            try
            {
                var project = await _context.Projects
                    .Include(p => p.BudgetParts)
                        .ThenInclude(bp => bp.Expenses)
                    .FirstOrDefaultAsync(p => p.ProjectId == id);

                if (project == null)
                {
                    _logger.LogWarning("DeactivateProject: Project with ID {Id} not found.", id);
                    return NotFound(new { success = false, message = $"Proyecto con ID {id} no encontrado." });
                }

                if (project.Status == "Inactive")
                {
                    _logger.LogWarning("DeactivateProject: Project with ID {Id} is already inactive.", id);
                    return BadRequest(new { success = false, message = $"El proyecto con ID {id} ya se encuentra inactivo." });
                }

                if (project.Status != "Active")
                {
                    _logger.LogWarning("DeactivateProject: Project with ID {Id} has status '{Status}', cannot deactivate from this state.", id, project.Status);
                    return BadRequest(new { success = false, message = $"El proyecto no está en estado 'Activo' y no puede ser desactivado. Estado actual: '{project.Status}'." });
                }

                project.Status = "Inactive";
                _logger.LogInformation("Deactivating Project ID {Id} (Name: {Name}).", project.ProjectId, project.ProjectName);

                var deactivatedBudgetParts = new List<BudgetPartResponseDTO>();
                var deactivatedExpensesInBudgetParts = new List<ExpenseResponseDTO>();

                foreach (var budgetPart in project.BudgetParts)
                {
                    if (budgetPart.Status == "Active") 
                    {
                        budgetPart.Status = "Inactive";
                        _logger.LogInformation("  Deactivating BudgetPart ID {BpId} (Name: {BpName}).", budgetPart.BudgetPartId, budgetPart.PartName);
                        deactivatedBudgetParts.Add(MapToBudgetPartDTO(budgetPart)); 
                    }

                    foreach (var expense in budgetPart.Expenses)
                    {
                        if (expense.Status == "Active") 
                        {
                            expense.Status = "Inactive";
                            _logger.LogInformation("    Deactivating Expense ID {ExpId} (Amount: {Amount}).", expense.ExpenseId, expense.ExpenseAmount);
                            deactivatedExpensesInBudgetParts.Add(new ExpenseResponseDTO
                            {
                                ExpenseId = expense.ExpenseId,
                                ExpenseAmount = expense.ExpenseAmount,
                                ExpenseDate = expense.ExpenseDate,
                                DocumentReference = expense.DocumentReference,
                                Description = expense.Description,
                                CreatedAt = expense.CreatedAt,
                                Status = expense.Status
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("DeactivateProject: Project ID {Id} and all associated budget parts/expenses successfully deactivated.", id);

                return Ok(new
                {
                    success = true,
                    message = $"Proyecto '{project.ProjectName}' (ID: {project.ProjectId}) y sus partidas/gastos asociados desactivados exitosamente.",
                    project = MapToProjectResponseDTO(project),
                    affectedBudgetParts = deactivatedBudgetParts,
                    affectedExpenses = deactivatedExpensesInBudgetParts
                });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "DeactivateProject: Database error deactivating project ID {Id}. Inner Exception: {InnerExceptionMessage}",
                                 id, dbEx.InnerException?.Message);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al desactivar el proyecto y sus elementos relacionados en la base de datos. Posible violación de restricción.",
                    detail = dbEx.Message,
                    innerError = dbEx.InnerException?.Message,
                    stackTrace = dbEx.InnerException?.StackTrace
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeactivateProject: Unexpected error deactivating project ID {Id}.", id);
                return StatusCode(500, new { success = false, message = "Error inesperado al desactivar el proyecto y sus elementos relacionados.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Mapea una entidad de partida presupuestaria a un objeto DTO de respuesta de partida presupuestaria, incluyendo sus propiedades y gastos asociados.
        /// </summary>
        /// <param name="b">La entidad de partida presupuestaria a mapear.</param>
        /// <returns>Un objeto <see cref="BudgetPartResponseDTO"/> que representa la partida presupuestaria y sus gastos relacionados.</returns>
        private static BudgetPartResponseDTO MapToBudgetPartDTO(BudgetPart b) => new()
        {
            BudgetPartId = b.BudgetPartId,
            PartName = b.PartName,
            AllocatedAmount = b.AllocatedAmount,
            RemainingAmount = b.RemainingAmount,
            CreatedAt = b.CreatedAt,

            Expenses = b.Expenses.Select(e => new ExpenseResponseDTO
            {
                ExpenseId = e.ExpenseId,
                ExpenseAmount = e.ExpenseAmount,
                ExpenseDate = e.ExpenseDate,
                DocumentReference = e.DocumentReference,
                Description = e.Description,
                CreatedAt = e.CreatedAt
            }).ToList()
        };

        /// <summary>
        /// Recupera todos los proyectos asignados a un gestor específico, basado en su correo electrónico.
        /// </summary>
        /// <param name="managerEmail">El correo electrónico del gestor.</param>
        /// <returns>Una lista de proyectos asignados al gestor o un mensaje de error en caso de fallo.</returns>
        [HttpGet("manager/{managerEmail}")]
        public async Task<IActionResult> GetProjectsByManager(string managerEmail)
        {
            try
            {
                var projects = await _context.Projects
                    .Where(p => p.ManagerEmail == managerEmail && p.Status == "Active")
                    .Include(p => p.Alerts)
                    .Include(p => p.BudgetParts).ThenInclude(b => b.Expenses)
                    .Include(p => p.ManagerEmailNavigation)
                    .ToListAsync();

                if (!projects.Any())
                {
                    return NotFound(new { success = false, message = $"No se encontraron proyectos para el gestor con el correo {managerEmail}." });
                }

                var dtoList = projects.Select(MapToProjectResponseDTO).ToList();

                return Ok(new { success = true, data = dtoList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocurrió un error al recuperar los proyectos del gestor con correo {ManagerEmail}.", managerEmail);
                return StatusCode(500, new { success = false, message = "Error al obtener proyectos del gestor.", detail = ex.Message });
            }
        }

    }
}
