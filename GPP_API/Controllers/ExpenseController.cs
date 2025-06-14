using GPP_API.DTO.BudgetPart;
using GPP_API.DTO.Expense; 
using GPP_API.DTO.Project;
using GPP_API.Models;
using GPP_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GPP_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExpenseController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ExpenseController> _logger;
        private readonly IEmailService _emailService;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ExpenseController"/>.
        /// </summary>
        /// <param name="context">El contexto de la base de datos para acceder a los datos de la aplicación.</param>
        /// <param name="logger">El servicio de registro para registrar información y errores.</param>
        /// <param name="emailService">El servicio de correo electrónico para enviar notificaciones.</param>
        public ExpenseController(ApplicationDbContext context, ILogger<ExpenseController> logger, IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
        }

        /// <summary>
        /// Mapea una entidad <see cref="Expense"/> a un objeto <see cref="ExpenseResponseDTO"/>.
        /// </summary>
        /// <param name="expense">La entidad de gasto a mapear.</param>
        /// <returns>Un objeto <see cref="ExpenseResponseDTO"/> que representa el gasto, o null si la entidad es nula.</returns>
        private ExpenseResponseDTO MapToExpenseResponseDTO(Expense expense)
        {
            if (expense == null) return null;

            return new ExpenseResponseDTO
            {
                ExpenseId = expense.ExpenseId,
                ExpenseAmount = expense.ExpenseAmount,
                Status = expense.Status,
                ExpenseDate = expense.ExpenseDate,
                DocumentReference = expense.DocumentReference,
                Description = expense.Description,
                CreatedAt = expense.CreatedAt
            };
        }

        /// <summary>
        /// Mapea una entidad <see cref="BudgetPart"/> a un objeto <see cref="AlertBudgetPartDTO"/>.
        /// </summary>
        /// <param name="budgetPart">La entidad de partida presupuestaria a mapear.</param>
        /// <returns>Un objeto <see cref="AlertBudgetPartDTO"/> que representa la partida presupuestaria, o null si la entidad es nula.</returns>
        private AlertBudgetPartDTO MapToAlertBudgetPartDTO(BudgetPart budgetPart)
        {
            if (budgetPart == null) return null;

            return new AlertBudgetPartDTO
            {
                BudgetPartId = budgetPart.BudgetPartId,
                PartName = budgetPart.PartName,
                AllocatedAmount = budgetPart.AllocatedAmount,
                RemainingAmount = budgetPart.RemainingAmount
            };
        }

        /// <summary>
        /// Mapea una entidad <see cref="Project"/> a un objeto <see cref="AlertProjectResponseDTO"/>.
        /// </summary>
        /// <param name="project">La entidad de proyecto a mapear.</param>
        /// <returns>Un objeto <see cref="AlertProjectResponseDTO"/> que representa el proyecto, o null si la entidad es nula.</returns>
        private AlertProjectResponseDTO MapToProjectResponseDTO(Project project)
        {
            if (project == null) return null;

            return new AlertProjectResponseDTO
            {
                ProjectId = project.ProjectId,
                ProjectName = project.ProjectName,
                Budget = project.Budget,
                RemainingBudget = project.RemainingBudget
            };
        }

        /// <summary>
        /// Crea un nuevo gasto asociado a una partida presupuestaria y actualiza los montos correspondientes.
        /// </summary>
        /// <param name="dto">Los datos necesarios para crear el gasto, incluyendo el ID de la partida presupuestaria y el monto del gasto.</param>
        /// <returns>Resultado de la acción que indica el éxito de la creación del gasto o un mensaje de error en caso de fallo.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseDTO dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("CreateExpense: Estado del modelo inválido. Errores: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(new { success = false, message = "Datos de gasto inválidos.", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
            }

            try
            {
                var budgetPart = await _context.BudgetParts
                    .Include(bp => bp.Project)
                    .FirstOrDefaultAsync(bp => bp.BudgetPartId == dto.BudgetPartId);

                var project = await _context.BudgetParts
                    .Where(bp => bp.BudgetPartId == dto.BudgetPartId)
                    .Select(bp => bp.Project)
                    .FirstOrDefaultAsync();

                var managerEmail = project.ManagerEmail;

                if (budgetPart == null)
                {
                    _logger.LogWarning("CreateExpense: Partida presupuestaria con ID {BudgetPartId} no encontrada.", dto.BudgetPartId);
                    return NotFound(new { success = false, message = $"Partida presupuestaria con ID {dto.BudgetPartId} no encontrada." });
                }

                if (budgetPart.Status != "Active")
                {
                    _logger.LogWarning("CreateExpense: Partida presupuestaria con ID {BudgetPartId} no está activa. Estado: {Status}", dto.BudgetPartId, budgetPart.Status);
                    return BadRequest(new { success = false, message = "La partida presupuestaria asociada no está activa. No se pueden registrar gastos." });
                }

                if (budgetPart.Project == null || budgetPart.Project.Status != "Active")
                {
                    _logger.LogWarning("CreateExpense: Proyecto de la partida presupuestaria {BudgetPartId} (ID: {ProjectId}) no está activo. Estado: {Status}", dto.BudgetPartId, budgetPart.ProjectId, budgetPart.Project?.Status);
                    return BadRequest(new { success = false, message = "El proyecto de la partida presupuestaria asociada no está activo. No se pueden registrar gastos." });
                }

                if (dto.ExpenseAmount <= 0)
                {
                    return BadRequest(new { success = false, message = "El monto del gasto debe ser un valor positivo." });
                }

                if (dto.ExpenseAmount > budgetPart.RemainingAmount)
                {
                    _logger.LogWarning("CreateExpense: Fondos insuficientes en la partida presupuestaria {BudgetPartId}. Restante: {Remaining}, Gasto intentado: {Amount}",
                        dto.BudgetPartId, budgetPart.RemainingAmount, dto.ExpenseAmount);
                    return BadRequest(new { success = false, message = $"Fondos insuficientes en la partida '{budgetPart.PartName}'. Monto restante: {budgetPart.RemainingAmount:N2}." });
                }

                var currentProjectRemainingBudget = budgetPart.Project.RemainingBudget;

                if (dto.ExpenseAmount > currentProjectRemainingBudget)
                {
                    _logger.LogWarning("CreateExpense: Fondos insuficientes en el presupuesto restante global del proyecto {ProjectId}. Restante: {Remaining}, Gasto intentado: {Amount}",
                        budgetPart.ProjectId, currentProjectRemainingBudget, dto.ExpenseAmount);
                    return BadRequest(new { success = false, message = $"Fondos insuficientes en el presupuesto restante total del proyecto '{budgetPart.Project.ProjectName}'. Monto restante: {currentProjectRemainingBudget:N2}. Este gasto lo sobrepasa." });
                }

                var newExpense = new Expense
                {
                    BudgetPartId = dto.BudgetPartId,
                    ProjectId = budgetPart.ProjectId,
                    ExpenseAmount = dto.ExpenseAmount,
                    ExpenseDate = dto.ExpenseDate,
                    DocumentReference = dto.DocumentReference,
                    Description = dto.Description,
                    CreatedAt = DateTime.UtcNow,
                    Status = "Active"
                };

                budgetPart.RemainingAmount -= newExpense.ExpenseAmount;

                if (budgetPart.Project != null)
                {
                    budgetPart.Project.RemainingBudget -= newExpense.ExpenseAmount;
                    _logger.LogInformation("Presupuesto restante del Proyecto {ProjectId} actualizado después del gasto. Nuevo presupuesto restante: {NewRemainingBudget}.", budgetPart.ProjectId, budgetPart.Project.RemainingBudget);
                }

                _context.Expenses.Add(newExpense);

                await _context.SaveChangesAsync();

                _logger.LogInformation("CreateExpense: Gasto ID {ExpenseId} creado exitosamente para la partida presupuestaria {BudgetPartId}. Presupuesto restante del proyecto actualizado.", newExpense.ExpenseId, newExpense.BudgetPartId);

                var projectResponseDto = MapToProjectResponseDTO(budgetPart.Project);
                var budgetResponseDto = MapToAlertBudgetPartDTO(budgetPart);
                var budgetPartPercentage = (budgetPart.RemainingAmount / budgetPart.AllocatedAmount) * 100;
                var projectPercentage = (budgetPart.Project.RemainingBudget / budgetPart.Project.Budget) * 100;

                if (budgetPartPercentage <= 60 && budgetPartPercentage > 40)
                {
                    await _emailService.SendBudgetPartPercentageEmail(managerEmail, budgetResponseDto, 60);
                }
                if (budgetPartPercentage <= 40 && budgetPartPercentage > 0)
                {
                    await _emailService.SendBudgetPartPercentageEmail(managerEmail, budgetResponseDto, 40);
                }
                if (budgetPartPercentage <= 0)
                {
                    await _emailService.SendBudgetPartPercentageEmail(managerEmail, budgetResponseDto, 0);
                }
                
                if (projectPercentage <= 50 && projectPercentage > 30)
                {
                    await _emailService.SendProjectBudgetPercentageEmail(managerEmail, projectResponseDto, 50);
                }
                if (projectPercentage <= 30 && projectPercentage > 0)
                {
                    await _emailService.SendProjectBudgetPercentageEmail(managerEmail, projectResponseDto, 30);
                }
                if (projectPercentage <= 0)
                {
                    await _emailService.SendProjectBudgetPercentageEmail(managerEmail, projectResponseDto, 0);
                }

                var resultDto = MapToExpenseResponseDTO(newExpense);
                return CreatedAtAction(nameof(GetExpenseById), new { id = newExpense.ExpenseId }, new { success = true, data = resultDto });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "CreateExpense: Error de base de datos al crear el gasto para la partida presupuestaria {BudgetPartId}. Excepción interna: {InnerExceptionMessage}",
                                        dto.BudgetPartId, dbEx.InnerException?.Message);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al guardar el gasto en la base de datos.",
                    detail = dbEx.Message,
                    innerError = dbEx.InnerException?.Message,
                    stackTrace = dbEx.InnerException?.StackTrace
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateExpense: Error inesperado al crear el gasto para la partida presupuestaria {BudgetPartId}.", dto.BudgetPartId);
                return StatusCode(500, new { success = false, message = "Error inesperado al crear el gasto.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Recupera todos los gastos activos asociados a partidas presupuestarias y proyectos activos.
        /// </summary>
        /// <returns>Una lista de gastos activos en formato DTO o un mensaje de error en caso de fallo.</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllExpenses()
        {
            try
            {
                var expenses = await _context.Expenses
                    .Include(e => e.BudgetPart)
                        .ThenInclude(bp => bp.Project)
                    .Where(e => e.Status == "Active" && 
                                e.BudgetPart.Status == "Active" &&
                                e.BudgetPart.Project.Status == "Active")
                    .ToListAsync();

                var dtoList = expenses.Select(MapToExpenseResponseDTO).ToList();

                _logger.LogInformation("GetAllExpenses: Se recuperaron {Count} gastos activos.", dtoList.Count);
                return Ok(new { success = true, data = dtoList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllExpenses: Error al recuperar los gastos.");
                return StatusCode(500, new { success = false, message = "Error al obtener la lista de gastos.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Recupera un gasto específico por su ID, asegurando que esté activo y asociado a partidas y proyectos activos.
        /// </summary>
        /// <param name="id">El ID del gasto a recuperar.</param>
        /// <returns>El gasto en formato DTO si se encuentra activo, o un mensaje de error si no se encuentra o no está activo.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetExpenseById(int id)
        {
            try
            {
                var expense = await _context.Expenses
                    .Include(e => e.BudgetPart)
                        .ThenInclude(bp => bp.Project)
                    .FirstOrDefaultAsync(e => e.ExpenseId == id &&
                                                 e.Status == "Active" &&
                                                 e.BudgetPart.Status == "Active" &&
                                                 e.BudgetPart.Project.Status == "Active");

                if (expense == null)
                {
                    _logger.LogWarning("GetExpenseById: Gasto con ID {Id} no encontrado o no activo.", id);
                    return NotFound(new { success = false, message = $"Gasto con ID {id} no encontrado o no está activo (o su partida/proyecto)." });
                }

                var dto = MapToExpenseResponseDTO(expense);
                _logger.LogInformation("GetExpenseById: Gasto recuperado con ID {Id}.", id);
                return Ok(new { success = true, data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetExpenseById: Error al recuperar el gasto con ID {Id}.", id);
                return StatusCode(500, new { success = false, message = "Error al obtener el gasto.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Recupera todos los gastos asociados a un proyecto específico.
        /// </summary>
        /// <param name="projectId">El ID del proyecto al que se le quieren recuperar los gastos.</param>
        /// <returns>Una lista de los gastos asociados al proyecto en formato DTO o un mensaje de error en caso de fallo.</returns>
        [HttpGet("project/{projectId}/expenses")]
        public async Task<IActionResult> GetExpensesByProject(int projectId)
        {
            try
            {
                var expenses = await _context.Expenses
                    .Include(e => e.BudgetPart)
                        .ThenInclude(bp => bp.Project)
                    .Where(e => e.Status == "Active" &&
                                e.BudgetPart.ProjectId == projectId &&
                                e.BudgetPart.Project.Status == "Active" &&
                                e.BudgetPart.Status == "Active")
                    .ToListAsync();

                var dtoList = expenses.Select(MapToExpenseResponseDTO).ToList();

                if (dtoList.Count == 0)
                {
                    return NotFound(new { success = false, message = "No se encontraron gastos activos para el proyecto." });
                }

                return Ok(new { success = true, data = dtoList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recuperar los gastos del proyecto con ID {ProjectId}.", projectId);
                return StatusCode(500, new { success = false, message = "Error al obtener los gastos del proyecto.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Recupera todos los gastos asociados a una partida presupuestaria específica.
        /// </summary>
        /// <param name="budgetPartId">El ID de la partida presupuestaria al que se le quieren recuperar los gastos.</param>
        /// <returns>Una lista de los gastos asociados a la partida presupuestaria en formato DTO o un mensaje de error en caso de fallo.</returns>
        [HttpGet("budgetpart/{budgetPartId}/expenses")]
        public async Task<IActionResult> GetExpensesByBudgetPart(int budgetPartId)
        {
            try
            {
                var expenses = await _context.Expenses
                    .Include(e => e.BudgetPart)
                        .ThenInclude(bp => bp.Project)
                    .Where(e => e.Status == "Active" &&
                                e.BudgetPart.BudgetPartId == budgetPartId &&
                                e.BudgetPart.Status == "Active" &&
                                e.BudgetPart.Project.Status == "Active")
                    .ToListAsync();

                var dtoList = expenses.Select(MapToExpenseResponseDTO).ToList();

                if (dtoList.Count == 0)
                {
                    return NotFound(new { success = false, message = "No se encontraron gastos activos para la partida presupuestaria." });
                }

                return Ok(new { success = true, data = dtoList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recuperar los gastos de la partida presupuestaria con ID {BudgetPartId}.", budgetPartId);
                return StatusCode(500, new { success = false, message = "Error al obtener los gastos de la partida presupuestaria.", detail = ex.Message });
            }
        }

    }
}