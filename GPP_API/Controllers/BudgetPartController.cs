using GPP_API.DTO.BudgetPart;
using GPP_API.DTO.Expense;
using GPP_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GPP_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BudgetPartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BudgetPartController> _logger;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="BudgetPartController"/>.
        /// </summary>
        /// <param name="context">El contexto de la base de datos para operaciones de acceso a datos.</param>
        /// <param name="logger">El servicio de registro para registrar información y errores.</param>
        public BudgetPartController(ApplicationDbContext context, ILogger<BudgetPartController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Recupera las partidas presupuestarias activas junto con sus gastos y proyectos asociados.
        /// </summary>
        /// <returns>Resultado de la acción que contiene la lista de partidas presupuestarias o un mensaje de error en caso de fallo.</returns>
        [HttpGet]
        public async Task<IActionResult> GetBudgeParts()
        {
            try
            {
                var budgetParts = await _context.BudgetParts
                    .Include(b => b.Expenses)
                    .Include(b => b.Project)
                    .Where(b => b.Status == "Active")
                    .ToListAsync();

                var dtoList = budgetParts.Select(MapToBudgetPartDTO).ToList();

                return Ok(new { success = true, data = dtoList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocurrió un error al recuperar las partidas presupuestarias.");
                return StatusCode(500, new { success = false, message = "Error al obtener las partidas presupuestarias.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Recupera una partida presupuestaria específica por su ID, incluyendo sus gastos y proyecto asociado.
        /// </summary>
        /// <param name="id">El ID de la partida presupuestaria a recuperar.</param>
        /// <returns>Resultado de la acción que contiene la partida presupuestaria o un mensaje de error si no se encuentra.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBudgetPartById(int id)
        {
            try
            {
                var budgetPart = await _context.BudgetParts
                    .Include(b => b.Expenses)
                    .Include(b => b.Project)
                    .FirstOrDefaultAsync(b => b.BudgetPartId == id && b.Status == "Active");

                if (budgetPart == null)
                {
                    return NotFound(new { success = false, message = $"Budget part with ID {id} not found or its associated project is not active." });
                }

                var dto = MapToBudgetPartDTO(budgetPart);

                return Ok(new { success = true, data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ocurrió un error al recuperar la partida presupuestaria con ID {id}.");
                return StatusCode(500, new { success = false, message = "Error al recuperar la partida presupuestaria.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Crea una nueva partida presupuestaria asociada a un proyecto específico.
        /// </summary>
        /// <param name="projectId">El ID del proyecto al que se asociará la nueva partida presupuestaria.</param>
        /// <param name="dto">Los datos necesarios para crear la partida presupuestaria.</param>
        /// <returns>Resultado de la acción que indica el éxito de la creación o un mensaje de error en caso de fallo.</returns>
        [HttpPost("{projectId}")]
        public async Task<IActionResult> CreateBudgetPart(int projectId, CreateBudgetPartDTO dto)
        {
            if (dto == null)
            {
                _logger.LogWarning("CreateBudgetPart: El DTO de la partida presupuestaria es nulo para el ID de proyecto {ProjectId}.", projectId);
                return BadRequest(new { success = false, message = "Se requieren los datos de la partida presupuestaria." });
            }

            try
            {
                var project = await _context.Projects
                    .Include(p => p.BudgetParts)
                    .FirstOrDefaultAsync(p => p.ProjectId == projectId);

                if (project == null)
                {
                    _logger.LogWarning("CreateBudgetPart: No se encontró el proyecto con ID {ProjectId}.", projectId);
                    return NotFound(new { success = false, message = $"No se encontró el proyecto con ID {projectId}." });
                }

                if (string.IsNullOrWhiteSpace(dto.PartName))
                {
                    _logger.LogWarning("CreateBudgetPart: El nombre de la partida presupuestaria es nulo o vacío para el ID de proyecto {ProjectId}.", projectId);
                    return BadRequest(new { success = false, message = "El nombre de la partida presupuestaria es obligatorio." });
                }

                var newBudgetPart = new BudgetPart
                {
                    ProjectId = projectId,
                    PartName = dto.PartName.Trim(),
                    AllocatedAmount = 0, 
                    RemainingAmount = 0,
                    CreatedAt = DateTime.UtcNow,
                    Status = "Active"
                };

                _context.BudgetParts.Add(newBudgetPart);

                await _context.SaveChangesAsync();

                var resultDto = MapToBudgetPartDTO(newBudgetPart);

                return CreatedAtAction(nameof(GetBudgetPartById), new { id = newBudgetPart.BudgetPartId }, new { success = true, data = resultDto });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "CreateBudgetPart: Ocurrió un DbUpdateException para el ID de proyecto {ProjectId}. Excepción interna: {InnerExceptionMessage}",
                                     projectId, dbEx.InnerException?.Message);

                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al guardar la partida presupuestaria en la base de datos. Posible violación de restricción.",
                    detail = dbEx.Message,
                    innerError = dbEx.InnerException?.Message,
                    stackTrace = dbEx.InnerException?.StackTrace
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateBudgetPart: Error inesperado al crear la partida presupuestaria para el ID de proyecto {ProjectId}.", projectId);
                return StatusCode(500, new { success = false, message = "Error inesperado al crear la partida presupuestaria.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Mapea una entidad <see cref="BudgetPart"/> a un objeto <see cref="BudgetPartResponseDTO"/>.
        /// </summary>
        /// <param name="b">La entidad de partida presupuestaria a mapear.</param>
        /// <returns>Un objeto <see cref="BudgetPartResponseDTO"/> que representa la partida presupuestaria.</returns>
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
        /// Transfiere fondos entre dos partidas presupuestarias dentro del mismo proyecto.
        /// </summary>
        /// <param name="dto">Los datos necesarios para realizar la transferencia, incluyendo los IDs de las partidas de origen y destino y el monto a transferir.</param>
        /// <returns>Resultado de la acción que indica el éxito de la transferencia o un mensaje de error en caso de fallo.</returns>
        [HttpPost("TransferFunds")]
        public async Task<IActionResult> TransferFunds(TransferFundsDTO dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("TransferFunds: Estado del modelo inválido. Errores: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(new { success = false, message = "Datos de transferencia inválidos.", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
            }

            try
            {
                if (dto.SourceBudgetPartId == dto.DestinationBudgetPartId)
                {
                    _logger.LogWarning("TransferFunds: Los IDs de la partida de origen y destino son iguales ({Id}).", dto.SourceBudgetPartId);
                    return BadRequest(new { success = false, message = "La partida de origen y la de destino no pueden ser la misma." });
                }

                var budgetParts = await _context.BudgetParts
                    .Include(b => b.Project)
                    .Where(b => b.BudgetPartId == dto.SourceBudgetPartId || b.BudgetPartId == dto.DestinationBudgetPartId)
                    .ToListAsync();

                var sourceBudgetPart = budgetParts.FirstOrDefault(b => b.BudgetPartId == dto.SourceBudgetPartId);
                var destinationBudgetPart = budgetParts.FirstOrDefault(b => b.BudgetPartId == dto.DestinationBudgetPartId);

                if (sourceBudgetPart == null)
                {
                    _logger.LogWarning("TransferFunds: No se encontró la partida de origen con ID {SourceId}.", dto.SourceBudgetPartId);
                    return NotFound(new { success = false, message = $"Partida presupuestaria de origen con ID {dto.SourceBudgetPartId} no encontrada." });
                }

                if (destinationBudgetPart == null)
                {
                    _logger.LogWarning("TransferFunds: No se encontró la partida de destino con ID {DestinationId}.", dto.DestinationBudgetPartId);
                    return NotFound(new { success = false, message = $"Partida presupuestaria de destino con ID {dto.DestinationBudgetPartId} no encontrada." });
                }

                if (sourceBudgetPart.ProjectId != destinationBudgetPart.ProjectId)
                {
                    _logger.LogWarning("TransferFunds: Las partidas {SourceId} (Proyecto {SourceProjId}) y {DestinationId} (Proyecto {DestProjId}) pertenecen a proyectos diferentes.",
                        dto.SourceBudgetPartId, sourceBudgetPart.ProjectId, dto.DestinationBudgetPartId, destinationBudgetPart.ProjectId);
                    return BadRequest(new { success = false, message = "Las partidas de origen y destino deben pertenecer al mismo proyecto." });
                }

                if (sourceBudgetPart.Project == null || sourceBudgetPart.Project.Status != "Active")
                {
                    _logger.LogWarning("TransferFunds: El proyecto de la partida de origen (ID: {ProjectId}) no está activo.", sourceBudgetPart.ProjectId);
                    return BadRequest(new { success = false, message = "El proyecto de la partida de origen no está activo. No se pueden transferir fondos." });
                }

                if (destinationBudgetPart.Project == null || destinationBudgetPart.Project.Status != "Active")
                {
                    _logger.LogWarning("TransferFunds: El proyecto de la partida de destino (ID: {ProjectId}) no está activo.", destinationBudgetPart.ProjectId);
                    return BadRequest(new { success = false, message = "El proyecto de la partida de destino no está activo. No se pueden transferir fondos." });
                }

                if (dto.Amount > sourceBudgetPart.RemainingAmount)
                {
                    _logger.LogWarning("TransferFunds: Fondos insuficientes en la partida de origen {SourceId}. Restante: {Remaining}, Intento de Transferencia: {Amount}",
                        dto.SourceBudgetPartId, sourceBudgetPart.RemainingAmount, dto.Amount);
                    return BadRequest(new { success = false, message = $"Monto insuficiente en la partida de origen. Monto restante: {sourceBudgetPart.RemainingAmount:N2}." });
                }

                sourceBudgetPart.RemainingAmount -= dto.Amount;
                destinationBudgetPart.RemainingAmount += dto.Amount;

                await _context.SaveChangesAsync();

                _logger.LogInformation("TransferFunds: Transferencia exitosa de {Amount} de la partida {SourceId} a la partida {DestinationId}.",
                    dto.Amount, dto.SourceBudgetPartId, dto.DestinationBudgetPartId);

                return Ok(new
                {
                    success = true,
                    message = "Fondos transferidos exitosamente.",
                    sourceBudgetPart = MapToBudgetPartDTO(sourceBudgetPart),
                    destinationBudgetPart = MapToBudgetPartDTO(destinationBudgetPart)
                });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "TransferFunds: Error de base de datos durante la transferencia de fondos de {SourceId} a {DestinationId}.", dto.SourceBudgetPartId, dto.DestinationBudgetPartId);
                return StatusCode(500, new { success = false, message = "Error al guardar los cambios en la base de datos durante la transferencia.", detail = dbEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TransferFunds: Error inesperado durante la transferencia de fondos de {SourceId} a {DestinationId}.", dto.SourceBudgetPartId, dto.DestinationBudgetPartId);
                return StatusCode(500, new { success = false, message = "Error inesperado al movilizar fondos.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Desactiva una partida presupuestaria y transfiere sus fondos a otra partida dentro del mismo proyecto.
        /// </summary>
        /// <param name="dto">Los datos necesarios para desactivar la partida y transferir los fondos, incluyendo los IDs de las partidas.</param>
        /// <returns>Resultado de la acción que indica el éxito de la desactivación y transferencia o un mensaje de error en caso de fallo.</returns>
        [HttpPost("DeactivateAndTransfer")]
        public async Task<IActionResult> DeactivateBudgetPart([FromBody] DeactivateBudgetPartDTO dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("DeactivateBudgetPart: Invalid model state. Errors: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(new { success = false, message = "Datos de desactivación inválidos.", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
            }

            try
            {
                if (dto.BudgetPartIdToDeactivate == dto.ReceivingBudgetPartId)
                {
                    _logger.LogWarning("DeactivateBudgetPart: Budget part to deactivate and receiving budget part IDs are the same ({Id}).", dto.BudgetPartIdToDeactivate);
                    return BadRequest(new { success = false, message = "La partida a desactivar no puede ser la misma que la partida receptora." });
                }

                var budgetParts = await _context.BudgetParts
                    .Include(b => b.Project)
                    .Where(b => b.BudgetPartId == dto.BudgetPartIdToDeactivate || b.BudgetPartId == dto.ReceivingBudgetPartId)
                    .ToListAsync();

                var budgetPartToDeactivate = budgetParts.FirstOrDefault(b => b.BudgetPartId == dto.BudgetPartIdToDeactivate);
                var receivingBudgetPart = budgetParts.FirstOrDefault(b => b.BudgetPartId == dto.ReceivingBudgetPartId);

                if (budgetPartToDeactivate == null)
                {
                    _logger.LogWarning("DeactivateBudgetPart: Budget part to deactivate with ID {DeactivateId} not found.", dto.BudgetPartIdToDeactivate);
                    return NotFound(new { success = false, message = $"Partida presupuestaria a desactivar con ID {dto.BudgetPartIdToDeactivate} no encontrada." });
                }

                if (receivingBudgetPart == null)
                {
                    _logger.LogWarning("DeactivateBudgetPart: Receiving budget part with ID {ReceivingId} not found.", dto.ReceivingBudgetPartId);
                    return NotFound(new { success = false, message = $"Partida presupuestaria receptora con ID {dto.ReceivingBudgetPartId} no encontrada." });
                }

                if (budgetPartToDeactivate.ProjectId != receivingBudgetPart.ProjectId)
                {
                    _logger.LogWarning("DeactivateBudgetPart: Budget parts {DeactivateId} (Project {DeactivateProjId}) and {ReceivingId} (Project {ReceivingProjId}) belong to different projects.",
                        dto.BudgetPartIdToDeactivate, budgetPartToDeactivate.ProjectId, dto.ReceivingBudgetPartId, receivingBudgetPart.ProjectId);
                    return BadRequest(new { success = false, message = "Las partidas a desactivar y receptora deben pertenecer al mismo proyecto." });
                }

                if (budgetPartToDeactivate.Project == null || budgetPartToDeactivate.Project.Status != "Active")
                {
                    _logger.LogWarning("DeactivateBudgetPart: Project of budget part to deactivate (ID: {ProjectId}) is not active.", budgetPartToDeactivate.ProjectId);
                    return BadRequest(new { success = false, message = "El proyecto de la partida a desactivar no está activo. No se puede proceder." });
                }

                if (receivingBudgetPart.Project == null || receivingBudgetPart.Project.Status != "Active")
                {
                    _logger.LogWarning("DeactivateBudgetPart: Project of receiving budget part (ID: {ProjectId}) is not active.", receivingBudgetPart.ProjectId);
                    return BadRequest(new { success = false, message = "El proyecto de la partida receptora no está activo. No se puede proceder." });
                }

                if (budgetPartToDeactivate.Status != "Active")
                {
                    _logger.LogWarning("DeactivateBudgetPart: Budget part with ID {DeactivateId} is already inactive (Status: {Status}).", dto.BudgetPartIdToDeactivate, budgetPartToDeactivate.Status);
                    return BadRequest(new { success = false, message = $"La partida presupuestaria con ID {dto.BudgetPartIdToDeactivate} ya se encuentra inactiva." });
                }

                decimal fundsToTransfer = budgetPartToDeactivate.RemainingAmount;

                receivingBudgetPart.RemainingAmount += fundsToTransfer;

                budgetPartToDeactivate.RemainingAmount = 0;

                budgetPartToDeactivate.Status = "Inactive";

                await _context.SaveChangesAsync();

                _logger.LogInformation("DeactivateBudgetPart: Budget part {DeactivateId} deactivated and {Funds} funds transferred to {ReceivingId}.",
                    dto.BudgetPartIdToDeactivate, fundsToTransfer, dto.ReceivingBudgetPartId);

                return Ok(new
                {
                    success = true,
                    message = $"Partida '{budgetPartToDeactivate.PartName}' (ID: {budgetPartToDeactivate.BudgetPartId}) desactivada exitosamente. {fundsToTransfer:N2} fondos transferidos a la partida '{receivingBudgetPart.PartName}' (ID: {receivingBudgetPart.BudgetPartId}).",
                    deactivatedBudgetPart = MapToBudgetPartDTO(budgetPartToDeactivate),
                    receivingBudgetPart = MapToBudgetPartDTO(receivingBudgetPart)
                });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "DeactivateBudgetPart: Database error during deactivation and fund transfer from {DeactivateId} to {ReceivingId}. Inner Exception: {InnerExceptionMessage}",
                    dto.BudgetPartIdToDeactivate, dto.ReceivingBudgetPartId, dbEx.InnerException?.Message);
                return StatusCode(500, new { success = false, message = "Error al guardar los cambios en la base de datos durante la desactivación y transferencia.", detail = dbEx.Message, innerError = dbEx.InnerException?.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeactivateBudgetPart: Unexpected error during deactivation and fund transfer from {DeactivateId} to {ReceivingId}.",
                    dto.BudgetPartIdToDeactivate, dto.ReceivingBudgetPartId);
                return StatusCode(500, new { success = false, message = "Error inesperado al desactivar la partida y movilizar fondos.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Actualiza el nombre de una partida presupuestaria específica.
        /// </summary>
        /// <param name="id">El ID de la partida presupuestaria a actualizar.</param>
        /// <param name="dto">Los datos necesarios para la actualización, incluyendo el nuevo nombre de la partida.</param>
        /// <returns>Resultado de la acción que indica el éxito de la actualización o un mensaje de error en caso de fallo.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBudgetPartName(int id, UpdateBudgetPartNameDTO dto) // Nombre del método cambiado
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("UpdateBudgetPartName: Invalid model state for ID {Id}. Errors: {Errors}", id, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(new { success = false, message = "Datos de actualización inválidos.", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
            }

            try
            {
                var budgetPart = await _context.BudgetParts
                    .Include(b => b.Project)
                    .FirstOrDefaultAsync(b => b.BudgetPartId == id);

                if (budgetPart == null)
                {
                    _logger.LogWarning("UpdateBudgetPartName: Budget part with ID {Id} not found.", id);
                    return NotFound(new { success = false, message = $"Partida presupuestaria con ID {id} no encontrada." });
                }

                if (budgetPart.Project == null || budgetPart.Project.Status != "Active")
                {
                    _logger.LogWarning("UpdateBudgetPartName: Project of budget part (ID: {ProjectId}) is not active.", budgetPart.ProjectId);
                    return BadRequest(new { success = false, message = "El proyecto asociado a esta partida no está activo. No se puede actualizar el nombre." });
                }

                budgetPart.PartName = dto.PartName.Trim();

                await _context.SaveChangesAsync();

                _logger.LogInformation("UpdateBudgetPartName: Budget part with ID {Id} name updated successfully.", id);

                return Ok(new { success = true, data = MapToBudgetPartDTO(budgetPart) });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "UpdateBudgetPartName: Database error updating budget part name ID {Id}. Inner Exception: {InnerExceptionMessage}",
                                     id, dbEx.InnerException?.Message);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al actualizar el nombre de la partida presupuestaria en la base de datos.",
                    detail = dbEx.Message,
                    innerError = dbEx.InnerException?.Message,
                    stackTrace = dbEx.InnerException?.StackTrace
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateBudgetPartName: Unexpected error updating budget part name ID {Id}.", id);
                return StatusCode(500, new { success = false, message = "Error inesperado al actualizar el nombre de la partida presupuestaria.", detail = ex.Message });
            }
        }
    }
}