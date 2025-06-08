using GPP_API.DTO.BudgetPart; // Para BudgetPartResponseDTO (si MapToBudgetPartDTO lo necesita)
using GPP_API.DTO.Expense; // Para ExpenseDTO y CreateExpenseDTO
using GPP_API.DTO.Project;
using GPP_API.Models; // Para las entidades Expense, BudgetPart, Project
using GPP_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GPP_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // La ruta base será /api/Expense
    public class ExpenseController : ControllerBase
    {
        private readonly ApplicationDbContext _context; // Reemplaza YourDbContext con el nombre de tu clase DbContext real
        private readonly ILogger<ExpenseController> _logger;
        private readonly IEmailService _emailService;

        public ExpenseController(ApplicationDbContext context, ILogger<ExpenseController> logger, IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
        }


        // --- Método auxiliar de mapeo de entidad a DTO para Expense ---
        // Se puede mover a DtoMappers si quieres centralizarlo.
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
        /// Creates a new expense record and links it to a specific budget part.
        /// It updates the remaining budget of that specific budget part,
        /// and recalculates the project's overall remaining budget by subtracting
        /// the amount of the newly registered expense from the project's remaining budget.
        /// </summary>
        /// <remarks>
        /// This endpoint allows for the creation of a new expense. The expense amount
        /// will be deducted from the 'RemainingAmount' of the associated budget part.
        /// The project's 'RemainingBudget' will be updated by subtracting the expense amount
        /// from the current RemainingBudget of the project.
        /// Strict validations are applied: the budget part must exist, both the budget part
        /// and its parent project must be "Active", the expense amount must be positive,
        /// and it must not exceed the remaining funds of the specific budget part
        /// or the project's overall remaining budget (calculated from all parts).
        /// </remarks>
        /// <param name="dto">A <see cref="CreateExpenseDTO"/> containing the details for the new expense.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> indicating the outcome of the operation.
        /// </returns>
        //[HttpPost]
        //public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseDTO dto)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        _logger.LogWarning("CreateExpense: Invalid model state. Errors: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
        //        return BadRequest(new { success = false, message = "Datos de gasto inválidos.", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
        //    }

        //    try
        //    {
        //        // Buscar la partida presupuestaria, incluyendo su proyecto
        //        var budgetPart = await _context.BudgetParts
        //            .Include(bp => bp.Project)
        //            .FirstOrDefaultAsync(bp => bp.BudgetPartId == dto.BudgetPartId);

        //        if (budgetPart == null)
        //        {
        //            _logger.LogWarning("CreateExpense: BudgetPart with ID {BudgetPartId} not found.", dto.BudgetPartId);
        //            return NotFound(new { success = false, message = $"Partida presupuestaria con ID {dto.BudgetPartId} no encontrada." });
        //        }

        //        // Validar que la partida y su proyecto estén activos
        //        if (budgetPart.Status != "Active")
        //        {
        //            _logger.LogWarning("CreateExpense: BudgetPart with ID {BudgetPartId} is not active. Status: {Status}", dto.BudgetPartId, budgetPart.Status);
        //            return BadRequest(new { success = false, message = "La partida presupuestaria asociada no está activa. No se pueden registrar gastos." });
        //        }

        //        if (budgetPart.Project == null || budgetPart.Project.Status != "Active")
        //        {
        //            _logger.LogWarning("CreateExpense: Project of BudgetPart {BudgetPartId} (ID: {ProjectId}) is not active. Status: {Status}", dto.BudgetPartId, budgetPart.ProjectId, budgetPart.Project?.Status);
        //            return BadRequest(new { success = false, message = "El proyecto de la partida presupuestaria asociada no está activo. No se pueden registrar gastos." });
        //        }

        //        // Validar que el monto del gasto sea positivo
        //        if (dto.ExpenseAmount <= 0)
        //        {
        //            return BadRequest(new { success = false, message = "El monto del gasto debe ser un valor positivo." });
        //        }

        //        // Validar fondos suficientes en la partida específica
        //        if (dto.ExpenseAmount > budgetPart.RemainingAmount)
        //        {
        //            _logger.LogWarning("CreateExpense: Insufficient funds in BudgetPart {BudgetPartId}. Remaining: {Remaining}, Attempted Expense: {Amount}",
        //                dto.BudgetPartId, budgetPart.RemainingAmount, dto.ExpenseAmount);
        //            return BadRequest(new { success = false, message = $"Fondos insuficientes en la partida '{budgetPart.PartName}'. Monto restante: {budgetPart.RemainingAmount:N2}." });
        //        }

        //        // Recalcular el RemainingBudget del proyecto ANTES de crear el gasto para la validación.
        //        var currentProjectRemainingBudget = budgetPart.Project.RemainingBudget;

        //        // NUEVA VALIDACIÓN: Validar fondos suficientes en el presupuesto restante GLOBAL del proyecto
        //        if (dto.ExpenseAmount > currentProjectRemainingBudget)
        //        {
        //            _logger.LogWarning("CreateExpense: Insufficient funds in Project {ProjectId} global RemainingBudget. Remaining: {Remaining}, Attempted Expense: {Amount}",
        //                budgetPart.ProjectId, currentProjectRemainingBudget, dto.ExpenseAmount);
        //            return BadRequest(new { success = false, message = $"Fondos insuficientes en el presupuesto restante total del proyecto '{budgetPart.Project.ProjectName}'. Monto restante: {currentProjectRemainingBudget:N2}. Este gasto lo sobrepasa." });
        //        }

        //        // Crear la entidad Expense
        //        var newExpense = new Expense
        //        {
        //            BudgetPartId = dto.BudgetPartId,
        //            ProjectId = budgetPart.ProjectId,
        //            ExpenseAmount = dto.ExpenseAmount,
        //            ExpenseDate = dto.ExpenseDate,
        //            DocumentReference = dto.DocumentReference,
        //            Description = dto.Description,
        //            CreatedAt = DateTime.UtcNow,
        //            Status = "Active"
        //        };

        //        // 1. Actualizar el monto restante de la partida presupuestaria (RESTAR el gasto)
        //        budgetPart.RemainingAmount -= newExpense.ExpenseAmount;

        //        // 2. Actualizar el RemainingBudget del proyecto restando el monto del gasto registrado
        //        if (budgetPart.Project != null)
        //        {
        //            budgetPart.Project.RemainingBudget -= newExpense.ExpenseAmount;  // Restamos el gasto del RemainingBudget del proyecto
        //            _logger.LogInformation("Project {ProjectId} RemainingBudget updated after expense. New RemainingBudget: {NewRemainingBudget}.", budgetPart.ProjectId, budgetPart.Project.RemainingBudget);
        //        }

        //        _context.Expenses.Add(newExpense);

        //        // Guardar todos los cambios en la base de datos
        //        await _context.SaveChangesAsync();

        //        _logger.LogInformation("CreateExpense: Expense ID {ExpenseId} created successfully for BudgetPart {BudgetPartId}. Project RemainingBudget updated.", newExpense.ExpenseId, newExpense.BudgetPartId);

        //        var resultDto = MapToExpenseResponseDTO(newExpense);
        //        return CreatedAtAction(nameof(GetExpenseById), new { id = newExpense.ExpenseId }, new { success = true, data = resultDto });
        //    }
        //    catch (DbUpdateException dbEx)
        //    {
        //        _logger.LogError(dbEx, "CreateExpense: Database error creating expense for BudgetPart {BudgetPartId}. Inner Exception: {InnerExceptionMessage}",
        //                            dto.BudgetPartId, dbEx.InnerException?.Message);
        //        return StatusCode(500, new
        //        {
        //            success = false,
        //            message = "Error al guardar el gasto en la base de datos.",
        //            detail = dbEx.Message,
        //            innerError = dbEx.InnerException?.Message,
        //            stackTrace = dbEx.InnerException?.StackTrace
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "CreateExpense: Unexpected error creating expense for BudgetPart {BudgetPartId}.", dto.BudgetPartId);
        //        return StatusCode(500, new { success = false, message = "Error inesperado al crear el gasto.", detail = ex.Message });
        //    }
        //}

        [HttpPost]
        public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseDTO dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("CreateExpense: Invalid model state. Errors: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(new { success = false, message = "Datos de gasto inválidos.", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
            }

            try
            {
                // Buscar la partida presupuestaria, incluyendo su proyecto
                var budgetPart = await _context.BudgetParts
                    .Include(bp => bp.Project)
                    .FirstOrDefaultAsync(bp => bp.BudgetPartId == dto.BudgetPartId);

                var project = await _context.BudgetParts
                    .Where(bp => bp.BudgetPartId == dto.BudgetPartId) // Usar el ID de la partida presupuestaria
                    .Select(bp => bp.Project) // Obtener el proyecto asociado
                    .FirstOrDefaultAsync();

                var managerEmail = project.ManagerEmail;

                if (budgetPart == null)
                {
                    _logger.LogWarning("CreateExpense: BudgetPart with ID {BudgetPartId} not found.", dto.BudgetPartId);
                    return NotFound(new { success = false, message = $"Partida presupuestaria con ID {dto.BudgetPartId} no encontrada." });
                }

                // Validar que la partida y su proyecto estén activos
                if (budgetPart.Status != "Active")
                {
                    _logger.LogWarning("CreateExpense: BudgetPart with ID {BudgetPartId} is not active. Status: {Status}", dto.BudgetPartId, budgetPart.Status);
                    return BadRequest(new { success = false, message = "La partida presupuestaria asociada no está activa. No se pueden registrar gastos." });
                }

                if (budgetPart.Project == null || budgetPart.Project.Status != "Active")
                {
                    _logger.LogWarning("CreateExpense: Project of BudgetPart {BudgetPartId} (ID: {ProjectId}) is not active. Status: {Status}", dto.BudgetPartId, budgetPart.ProjectId, budgetPart.Project?.Status);
                    return BadRequest(new { success = false, message = "El proyecto de la partida presupuestaria asociada no está activo. No se pueden registrar gastos." });
                }

                // Validar que el monto del gasto sea positivo
                if (dto.ExpenseAmount <= 0)
                {
                    return BadRequest(new { success = false, message = "El monto del gasto debe ser un valor positivo." });
                }

                // Validar fondos suficientes en la partida específica
                if (dto.ExpenseAmount > budgetPart.RemainingAmount)
                {
                    _logger.LogWarning("CreateExpense: Insufficient funds in BudgetPart {BudgetPartId}. Remaining: {Remaining}, Attempted Expense: {Amount}",
                        dto.BudgetPartId, budgetPart.RemainingAmount, dto.ExpenseAmount);
                    return BadRequest(new { success = false, message = $"Fondos insuficientes en la partida '{budgetPart.PartName}'. Monto restante: {budgetPart.RemainingAmount:N2}." });
                }

                // Recalcular el RemainingBudget del proyecto ANTES de crear el gasto para la validación.
                var currentProjectRemainingBudget = budgetPart.Project.RemainingBudget;

                // NUEVA VALIDACIÓN: Validar fondos suficientes en el presupuesto restante GLOBAL del proyecto
                if (dto.ExpenseAmount > currentProjectRemainingBudget)
                {
                    _logger.LogWarning("CreateExpense: Insufficient funds in Project {ProjectId} global RemainingBudget. Remaining: {Remaining}, Attempted Expense: {Amount}",
                        budgetPart.ProjectId, currentProjectRemainingBudget, dto.ExpenseAmount);
                    return BadRequest(new { success = false, message = $"Fondos insuficientes en el presupuesto restante total del proyecto '{budgetPart.Project.ProjectName}'. Monto restante: {currentProjectRemainingBudget:N2}. Este gasto lo sobrepasa." });
                }

                // Crear la entidad Expense
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

                // 1. Actualizar el monto restante de la partida presupuestaria (RESTAR el gasto)
                budgetPart.RemainingAmount -= newExpense.ExpenseAmount;

                // 2. Actualizar el RemainingBudget del proyecto restando el monto del gasto registrado
                if (budgetPart.Project != null)
                {
                    budgetPart.Project.RemainingBudget -= newExpense.ExpenseAmount;  // Restamos el gasto del RemainingBudget del proyecto
                    _logger.LogInformation("Project {ProjectId} RemainingBudget updated after expense. New RemainingBudget: {NewRemainingBudget}.", budgetPart.ProjectId, budgetPart.Project.RemainingBudget);
                }

                _context.Expenses.Add(newExpense);

                // Guardar todos los cambios en la base de datos
                await _context.SaveChangesAsync();

                _logger.LogInformation("CreateExpense: Expense ID {ExpenseId} created successfully for BudgetPart {BudgetPartId}. Project RemainingBudget updated.", newExpense.ExpenseId, newExpense.BudgetPartId);

                // Mapeamos el proyecto a ProjectResponseDTO
                var projectResponseDto = MapToProjectResponseDTO(budgetPart.Project);
                var budgetResponseDto = MapToAlertBudgetPartDTO(budgetPart);

                // Verificar los umbrales de porcentaje y enviar correos electrónicos
                var budgetPartPercentage = (budgetPart.RemainingAmount / budgetPart.AllocatedAmount) * 100;
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

                // Para el proyecto (50%, 30%, 0%)
                var projectPercentage = (budgetPart.Project.RemainingBudget / budgetPart.Project.Budget) * 100;
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
                _logger.LogError(dbEx, "CreateExpense: Database error creating expense for BudgetPart {BudgetPartId}. Inner Exception: {InnerExceptionMessage}",
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
                _logger.LogError(ex, "CreateExpense: Unexpected error creating expense for BudgetPart {BudgetPartId}.", dto.BudgetPartId);
                return StatusCode(500, new { success = false, message = "Error inesperado al crear el gasto.", detail = ex.Message });
            }
        }


        /// <summary>
        /// Retrieves a list of all active expenses, including those associated with active budget parts and projects.
        /// </summary>
        /// <remarks>
        /// This endpoint fetches all expense records that are currently "Active",
        /// and belong to a budget part which is "Active", and that budget part
        /// belongs to a project which is also "Active".
        /// Expenses are mapped to <see cref="ExpenseResponseDTO"/> for API consumption.
        /// </remarks>
        /// <returns>
        /// A <see cref="Task{TResult}"/> containing an <see cref="IActionResult"/>
        /// with a list of <see cref="ExpenseResponseDTO"/> objects if successful.
        /// </returns>
        [HttpGet]
        public async Task<IActionResult> GetAllExpenses()
        {
            try
            {
                var expenses = await _context.Expenses
                    .Include(e => e.BudgetPart)
                        .ThenInclude(bp => bp.Project) // Incluir el proyecto para filtrar por su estado
                    .Where(e => e.Status == "Active" &&
                                e.BudgetPart.Status == "Active" &&
                                e.BudgetPart.Project.Status == "Active")
                    .ToListAsync();

                var dtoList = expenses.Select(MapToExpenseResponseDTO).ToList();

                _logger.LogInformation("GetAllExpenses: Retrieved {Count} active expenses.", dtoList.Count);
                return Ok(new { success = true, data = dtoList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllExpenses: Error retrieving expenses.");
                return StatusCode(500, new { success = false, message = "Error al obtener la lista de gastos.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves a specific expense by its ID, if it is active and belongs to an active budget part and project.
        /// </summary>
        /// <remarks>
        /// This endpoint fetches a single expense record by its unique identifier.
        /// The expense is returned only if it is "Active", its associated budget part is "Active",
        /// and that budget part's parent project is "Active".
        /// The retrieved entity is mapped to an <see cref="ExpenseResponseDTO"/>.
        /// </remarks>
        /// <param name="id">The unique identifier of the expense to retrieve.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> containing an <see cref="IActionResult"/>:
        /// <list type="bullet">
        ///   <item><description><see cref="Microsoft.AspNetCore.Mvc.OkObjectResult"/> (HTTP 200 OK) with the <see cref="ExpenseResponseDTO"/>.</description></item>
        ///   <item><description><see cref="Microsoft.AspNetCore.Mvc.NotFoundResult"/> (HTTP 404 Not Found) if the expense is not found
        ///   or does not meet the "Active" status criteria for itself, its budget part, or its project.</description></item>
        ///   <item><description><see cref="Microsoft.AspNetCore.Mvc.StatusCodeResult"/> (HTTP 500 Internal Server Error) for unexpected errors.</description></item>
        /// </list>
        /// </returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetExpenseById(int id)
        {
            try
            {
                var expense = await _context.Expenses
                    .Include(e => e.BudgetPart)
                        .ThenInclude(bp => bp.Project) // Incluir el proyecto para verificar su estado
                    .FirstOrDefaultAsync(e => e.ExpenseId == id &&
                                            e.Status == "Active" &&
                                            e.BudgetPart.Status == "Active" &&
                                            e.BudgetPart.Project.Status == "Active");

                if (expense == null)
                {
                    _logger.LogWarning("GetExpenseById: Expense with ID {Id} not found or not active.", id);
                    return NotFound(new { success = false, message = $"Gasto con ID {id} no encontrado o no está activo (o su partida/proyecto)." });
                }

                var dto = MapToExpenseResponseDTO(expense);
                _logger.LogInformation("GetExpenseById: Retrieved expense with ID {Id}.", id);
                return Ok(new { success = true, data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetExpenseById: Error retrieving expense with ID {Id}.", id);
                return StatusCode(500, new { success = false, message = "Error al obtener el gasto.", detail = ex.Message });
            }
        }
    }
}