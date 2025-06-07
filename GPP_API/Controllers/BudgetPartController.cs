using GPP_API.DTO.BudgetPart;
using GPP_API.DTO.Expense;
using GPP_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GPP_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BudgetPartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BudgetPartController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BudgetPartController"/> class.
        /// </summary>
        /// <param name="context">The database context, injected via dependency injection,
        /// used for interacting with the application's data store related to budget parts.</param>
        /// <param name="logger">The logger instance, injected via dependency injection,
        /// used for logging messages and events originating from the BudgetPartController.</param>
        /// <remarks>
        /// This constructor leverages dependency injection to provide the necessary services
        /// (<see cref="ApplicationDbContext"/> and <see cref="ILogger{TCategoryName}"/>)
        /// to the controller, promoting best practices such as loose coupling and testability.
        /// </remarks>
        public BudgetPartController(ApplicationDbContext context, ILogger<BudgetPartController> logger)
        {
            // Assign the injected ApplicationDbContext to a private field for database operations related to budget parts.
            _context = context;
            // Assign the injected ILogger to a private field for logging purposes within this controller.
            _logger = logger;
        }

        /// <summary>
        /// Asynchronously retrieves budget parts associated with projects that have an "Active" status,
        /// including their associated expenses, from the database.
        /// </summary>
        /// <remarks>
        /// This endpoint fetches budget part records only for projects where the status is "Active".
        /// It eagerly loads all related expense entities for each budget part to provide a complete
        /// view of budgetary allocations and their expenditures. The retrieved entities are then
        /// mapped to Data Transfer Objects (DTOs) for API consumption, ensuring that only relevant
        /// data is exposed to the client.
        /// </remarks>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// The task result contains an <see cref="IActionResult"/>:
        /// <list type="bullet">
        ///   <item><description><see cref="Microsoft.AspNetCore.Mvc.OkObjectResult"/> (HTTP 200 OK) with a success flag
        ///   and a list of <see cref="BudgetPartDTO"/> objects.</description></item>
        ///   <item><description><see cref="Microsoft.AspNetCore.Mvc.StatusCodeResult"/> (HTTP 500 Internal Server Error)
        ///   with a failure flag and an error message if an unexpected error occurs during data retrieval.</description></item>
        /// </list>
        /// </returns>
        [HttpGet]
        public async Task<IActionResult> GetBudgeParts()
        {
            try
            {
                // Query the database for budget parts, including their expenses,
                // but ONLY for projects with Status == "Active".
                var budgetParts = await _context.BudgetParts
                    .Include(b => b.Expenses) // Include associated Expense entities
                    .Include(b => b.Project)  // Include the Project navigation property
                    .Where(b => b.Status == "Active") // Filter by Status
                    .ToListAsync();

                // Map the retrieved BudgetPart entities to BudgetPartDTOs.
                var dtoList = budgetParts.Select(MapToBudgetPartDTO).ToList();

                // Return a 200 OK response with the list of DTOs.
                return Ok(new { success = true, data = dtoList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving budget parts.");
                // Return a 500 Internal Server Error response.
                return StatusCode(500, new { success = false, message = "Error al obtener las partidas presupuestarias.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Asynchronously retrieves a specific budget part by its ID, including its associated expenses,
        /// only if it belongs to a project that has an "Active" status.
        /// </summary>
        /// <remarks>
        /// This endpoint fetches a single budget part record identified by <paramref name="id"/>.
        /// It eagerly loads all related expense entities for the budget part. Crucially, it only returns
        /// the budget part if its parent project's status is "Active". This ensures that only relevant
        /// budgetary allocations within active projects are accessible.
        /// The retrieved entity is then mapped to a Data Transfer Object (DTO) for API consumption.
        /// </remarks>
        /// <param name="id">The unique identifier of the budget part to retrieve.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// The task result contains an <see cref="IActionResult"/>:
        /// <list type="bullet">
        ///   <item><description><see cref="Microsoft.AspNetCore.Mvc.OkObjectResult"/> (HTTP 200 OK) with a success flag
        ///   and the <see cref="BudgetPartDTO"/> object if found and active.</description></item>
        ///   <item><description><see cref="Microsoft.AspNetCore.Mvc.NotFoundResult"/> (HTTP 404 Not Found) with a failure flag
        ///   and a message if the budget part with the given ID is not found, or if it's found but its associated project is not "Active".</description></item>
        ///   <item><description><see cref="Microsoft.AspNetCore.Mvc.StatusCodeResult"/> (HTTP 500 Internal Server Error)
        ///   with a failure flag and an error message if an unexpected error occurs during data retrieval.</description></item>
        /// </list>
        /// </returns>
        // GET: api/BudgetParts/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBudgetPartById(int id)
        {
            try
            {
                // Search for the budget part by ID, including its related expenses and the parent project.
                // It also filters to ensure the parent project has an "Active" status.
                var budgetPart = await _context.BudgetParts
                    .Include(b => b.Expenses) // Includes associated Expense entities
                    .Include(b => b.Project)   // <--- Includes the parent Project
                    .FirstOrDefaultAsync(b => b.BudgetPartId == id && b.Status == "Active"); // <--- FILTERS by ID AND by the parent Project's Status

                // Check if the budget part exists AND if it met the "Active" status filter
                if (budgetPart == null)
                {
                    // Returns 404 Not Found if it doesn't exist or if the project is not active.
                    return NotFound(new { success = false, message = $"Budget part with ID {id} not found or its associated project is not active." });
                }

                // Map the entity to a DTO to expose only the necessary information
                var dto = MapToBudgetPartDTO(budgetPart);

                // Returns 200 OK with the found budget part
                return Ok(new { success = true, data = dto });
            }
            catch (Exception ex)
            {
                // Log the exception for debugging and monitoring purposes.
                _logger.LogError(ex, $"An error occurred while retrieving budget part with ID {id}.");

                // Returns 500 Internal Server Error if an unexpected failure occurs
                return StatusCode(500, new { success = false, message = "Error retrieving budget part.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Creates a new budget part for a specific project, initializing its allocated amount to zero.
        /// </summary>
        /// <param name="projectId">ID of the project.</param>
        /// <param name="dto">Data for the new budget part (PartName).</param>
        /// <returns>Created budget part data or an error response.</returns>
        [HttpPost("{projectId}")]
        public async Task<IActionResult> CreateBudgetPart(int projectId, CreateBudgetPartDTO dto)
        {
            if (dto == null)
            {
                _logger.LogWarning("CreateBudgetPart: Budget part DTO is null for project ID {ProjectId}.", projectId);
                return BadRequest(new { success = false, message = "Budget part data is required." });
            }

            try
            {
                var project = await _context.Projects
                    .Include(p => p.BudgetParts)
                    .FirstOrDefaultAsync(p => p.ProjectId == projectId);

                if (project == null)
                {
                    _logger.LogWarning("CreateBudgetPart: Project with ID {ProjectId} not found.", projectId);
                    return NotFound(new { success = false, message = $"Project with id {projectId} not found." });
                }

                if (string.IsNullOrWhiteSpace(dto.PartName))
                {
                    _logger.LogWarning("CreateBudgetPart: Budget part name is null or empty for project ID {ProjectId}.", projectId);
                    return BadRequest(new { success = false, message = "The budget part name is mandatory." });
                }

                var newBudgetPart = new BudgetPart
                {
                    ProjectId = projectId,
                    PartName = dto.PartName.Trim(),
                    AllocatedAmount = 0, // Automatically set to 0
                    RemainingAmount = 0, // Also set to 0
                    CreatedAt = DateTime.UtcNow,
                    Status = "Active" // Assuming BudgetPart also has a Status field
                };

                _context.BudgetParts.Add(newBudgetPart);

                // Project's total and remaining budget are NOT updated here
                // as the new budget part is created with an AllocatedAmount of 0.

                await _context.SaveChangesAsync();

                var resultDto = MapToBudgetPartDTO(newBudgetPart);

                return CreatedAtAction(nameof(GetBudgetPartById), new { id = newBudgetPart.BudgetPartId }, new { success = true, data = resultDto });
            }
            catch (DbUpdateException dbEx)
            {
                // Log the full exception details for server-side logging
                _logger.LogError(dbEx, "CreateBudgetPart: DbUpdateException occurred for project ID {ProjectId}. Inner Exception: {InnerExceptionMessage}",
                                 projectId, dbEx.InnerException?.Message);

                // *** THIS IS THE CRUCIAL CHANGE FOR YOUR API RESPONSE ***
                // Return the inner exception message to the client for debugging
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al guardar la partida presupuestaria en la base de datos. Posible violación de restricción.",
                    detail = dbEx.Message, // General EF Core message
                    innerError = dbEx.InnerException?.Message, // <--- THIS IS THE MESSAGE YOU NEED TO SEE!
                    stackTrace = dbEx.InnerException?.StackTrace // Useful in development environments
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateBudgetPart: Unexpected error creating budget part for project ID {ProjectId}.", projectId);
                return StatusCode(500, new { success = false, message = "Unexpected error creating budget part.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Maps a <see cref="BudgetPart"/> entity object to a <see cref="BudgetPartResponseDTO"/> data transfer object.
        /// </summary>
        /// <param name="b">The <see cref="BudgetPart"/> entity to be mapped.</param>
        /// <returns>
        /// A new <see cref="BudgetPartResponseDTO"/> instance populated with data from the provided <see cref="BudgetPart"/> entity,
        /// including a nested list of <see cref="ExpenseDTO"/> for its associated expenses.
        /// </returns>
        /// <remarks>
        /// This **static mapping method** is essential for presenting budget part data through the API. It ensures that
        /// the internal database model is cleanly separated from the external API contract, enhancing **security, data consistency,
        /// and flexibility**. The method performs a **deep mapping**, converting both the budget part's direct properties
        /// and its associated collection of <see cref="Expense"/> entities into their corresponding DTO representations.
        /// This approach prevents accidental exposure of sensitive internal data and allows for tailored API responses.
        /// </remarks>
        private static BudgetPartResponseDTO MapToBudgetPartDTO(BudgetPart b) => new()
        {
            // Directly map fundamental properties from the BudgetPart entity to the DTO.
            BudgetPartId = b.BudgetPartId,
            PartName = b.PartName,
            AllocatedAmount = b.AllocatedAmount,
            RemainingAmount = b.RemainingAmount,
            CreatedAt = b.CreatedAt,

            // Perform a nested mapping for the 'Expenses' collection.
            // Each Expense entity related to the BudgetPart is transformed into an ExpenseDTO.
            Expenses = b.Expenses.Select(e => new ExpenseDTO
            {
                ExpenseId = e.ExpenseId,
                ExpenseAmount = e.ExpenseAmount,
                ExpenseDate = e.ExpenseDate,
                DocumentReference = e.DocumentReference,
                Description = e.Description,
                CreatedAt = e.CreatedAt
            }).ToList() // Convert the resulting enumerable of ExpenseDTOs into a List.
        };

        /// <summary>
        /// Moves funds from one budget part to another within the SAME project, updating their remaining amounts.
        /// </summary>
        /// <remarks>
        /// This endpoint facilitates the transfer of a specified amount from a source budget part
        /// to a destination budget part. Both budget parts must belong to the same project, exist,
        /// belong to active projects, and the transfer amount must be positive and not exceed
        /// the remaining funds of the source budget part.
        /// The 'AllocatedAmount' of the budget parts remains unchanged; only the 'RemainingAmount' is adjusted.
        /// </remarks>
        /// <param name="dto">A <see cref="TransferFundsDTO"/> containing the source, destination, and amount for the transfer.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> indicating the outcome of the operation:
        /// <list type="bullet">
        ///   <item><description><see cref="Microsoft.AspNetCore.Mvc.OkObjectResult"/> (HTTP 200 OK) with a success message and
        ///   the updated DTOs of both affected budget parts if the transfer is successful.</description></item>
        ///   <item><description><see cref="Microsoft.AspNetCore.Mvc.BadRequestResult"/> (HTTP 400 Bad Request) with a failure message
        ///   for various validation errors (e.g., invalid amount, source equals destination, insufficient funds,
        ///   or budget parts belong to different projects).</description></item>
        ///   <item><description><see cref="Microsoft.AspNetCore.Mvc.NotFoundResult"/> (HTTP 404 Not Found) with a failure message
        ///   if either the source or destination budget part (or their associated project) does not exist or is not active.</description></item>
        ///   <item><description><see cref="Microsoft.AspNetCore.Mvc.StatusCodeResult"/> (HTTP 500 Internal Server Error) with a failure message
        ///   for unexpected server errors during the process.</description></item>
        /// </list>
        /// </returns>
        [HttpPost("TransferFunds")]
        public async Task<IActionResult> TransferFunds(TransferFundsDTO dto)
        {
            // Basic DTO validation (uses validation attributes)
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("TransferFunds: Invalid model state. Errors: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(new { success = false, message = "Datos de transferencia inválidos.", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
            }

            try
            {
                // Validate that the source and destination budget part IDs are not the same
                if (dto.SourceBudgetPartId == dto.DestinationBudgetPartId)
                {
                    _logger.LogWarning("TransferFunds: Source and destination budget part IDs are the same ({Id}).", dto.SourceBudgetPartId);
                    return BadRequest(new { success = false, message = "La partida de origen y la de destino no pueden ser la misma." });
                }

                // Retrieve both source and destination budget parts, including their projects
                // This is crucial for checking project status and ensuring they belong to the same project.
                var budgetParts = await _context.BudgetParts
                    .Include(b => b.Project)
                    .Where(b => b.BudgetPartId == dto.SourceBudgetPartId || b.BudgetPartId == dto.DestinationBudgetPartId)
                    .ToListAsync();

                var sourceBudgetPart = budgetParts.FirstOrDefault(b => b.BudgetPartId == dto.SourceBudgetPartId);
                var destinationBudgetPart = budgetParts.FirstOrDefault(b => b.BudgetPartId == dto.DestinationBudgetPartId);

                // Validate existence of both budget parts
                if (sourceBudgetPart == null)
                {
                    _logger.LogWarning("TransferFunds: Source budget part with ID {SourceId} not found.", dto.SourceBudgetPartId);
                    return NotFound(new { success = false, message = $"Partida presupuestaria de origen con ID {dto.SourceBudgetPartId} no encontrada." });
                }

                if (destinationBudgetPart == null)
                {
                    _logger.LogWarning("TransferFunds: Destination budget part with ID {DestinationId} not found.", dto.DestinationBudgetPartId);
                    return NotFound(new { success = false, message = $"Partida presupuestaria de destino con ID {dto.DestinationBudgetPartId} no encontrada." });
                }

                // --- NUEVA VALIDACIÓN: Ambas partidas deben ser del MISMO proyecto ---
                if (sourceBudgetPart.ProjectId != destinationBudgetPart.ProjectId)
                {
                    _logger.LogWarning("TransferFunds: Budget parts {SourceId} (Project {SourceProjId}) and {DestinationId} (Project {DestProjId}) belong to different projects.",
                        dto.SourceBudgetPartId, sourceBudgetPart.ProjectId, dto.DestinationBudgetPartId, destinationBudgetPart.ProjectId);
                    return BadRequest(new { success = false, message = "Las partidas de origen y destino deben pertenecer al mismo proyecto." });
                }

                // Validate that both budget parts' associated projects are active
                if (sourceBudgetPart.Project == null || sourceBudgetPart.Project.Status != "Active")
                {
                    _logger.LogWarning("TransferFunds: Source budget part's project (ID: {ProjectId}) is not active.", sourceBudgetPart.ProjectId);
                    return BadRequest(new { success = false, message = "El proyecto de la partida de origen no está activo. No se pueden transferir fondos." });
                }

                if (destinationBudgetPart.Project == null || destinationBudgetPart.Project.Status != "Active")
                {
                    _logger.LogWarning("TransferFunds: Destination budget part's project (ID: {ProjectId}) is not active.", destinationBudgetPart.ProjectId);
                    return BadRequest(new { success = false, message = "El proyecto de la partida de destino no está activo. No se pueden transferir fondos." });
                }

                // Validate that the transfer amount is less than or equal to the source budget part's remaining amount
                if (dto.Amount > sourceBudgetPart.RemainingAmount)
                {
                    _logger.LogWarning("TransferFunds: Insufficient funds in source budget part {SourceId}. Remaining: {Remaining}, Attempted Transfer: {Amount}",
                        dto.SourceBudgetPartId, sourceBudgetPart.RemainingAmount, dto.Amount);
                    return BadRequest(new { success = false, message = $"Monto insuficiente en la partida de origen. Monto restante: {sourceBudgetPart.RemainingAmount:N2}." });
                }

                // --- Perform the transfer ---
                sourceBudgetPart.RemainingAmount -= dto.Amount;
                destinationBudgetPart.RemainingAmount += dto.Amount;

                // Save changes to the database
                await _context.SaveChangesAsync();

                _logger.LogInformation("TransferFunds: Successfully transferred {Amount} from BudgetPart {SourceId} to BudgetPart {DestinationId}.",
                    dto.Amount, dto.SourceBudgetPartId, dto.DestinationBudgetPartId);

                // Return a successful response with the updated data of both budget parts (optional, but useful)
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
                _logger.LogError(dbEx, "TransferFunds: Database error during fund transfer from {SourceId} to {DestinationId}.", dto.SourceBudgetPartId, dto.DestinationBudgetPartId);
                return StatusCode(500, new { success = false, message = "Error al guardar los cambios en la base de datos durante la transferencia.", detail = dbEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TransferFunds: Unexpected error during fund transfer from {SourceId} to {DestinationId}.", dto.SourceBudgetPartId, dto.DestinationBudgetPartId);
                return StatusCode(500, new { success = false, message = "Error inesperado al movilizar fondos.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Deactivates a specific budget part and transfers all its remaining funds to another designated budget part.
        /// </summary>
        /// <remarks>
        /// This endpoint sets the status of the specified budget part to "Inactive" and transfers
        /// its entire <see cref="BudgetPart.RemainingAmount"/> to a <paramref name="dto.ReceivingBudgetPartId"/>.
        /// Both budget parts must exist, belong to the same active project, and the budget part to deactivate
        /// must currently be active. The 'AllocatedAmount' of both budget parts remains unchanged.
        /// </remarks>
        /// <param name="dto">A <see cref="DeactivateBudgetPartDTO"/> containing the ID of the budget part to deactivate
        /// and the ID of the budget part that will receive the funds.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> indicating the outcome of the operation:
        /// <list type="bullet">
        ///   <item><description><see cref="Microsoft.AspNetCore.Mvc.OkObjectResult"/> (HTTP 200 OK) with a success message
        ///   and the updated DTOs of both affected budget parts if the operation is successful.</description></item>
        ///   <item><description><see cref="Microsoft.AspNetCore.Mvc.BadRequestResult"/> (HTTP 400 Bad Request) with a failure message
        ///   for various validation errors (e.g., invalid IDs, same IDs, different projects, inactive project/budget part).</description></item>
        ///   <item><description><see cref="Microsoft.AspNetCore.Mvc.NotFoundResult"/> (HTTP 404 Not Found) with a failure message
        ///   if either of the specified budget parts does not exist.</description></item>
        ///   <item><description><see cref="Microsoft.AspNetCore.Mvc.StatusCodeResult"/> (HTTP 500 Internal Server Error) with a failure message
        ///   for unexpected server errors during the process.</description></item>
        /// </list>
        /// </returns>
        [HttpPost("DeactivateAndTransfer")] // Una ruta específica para esta acción
        public async Task<IActionResult> DeactivateBudgetPart([FromBody] DeactivateBudgetPartDTO dto)
        {
            // Validación inicial del DTO
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("DeactivateBudgetPart: Invalid model state. Errors: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(new { success = false, message = "Datos de desactivación inválidos.", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
            }

            try
            {
                // Validar que la partida a desactivar y la receptora no sean la misma
                if (dto.BudgetPartIdToDeactivate == dto.ReceivingBudgetPartId)
                {
                    _logger.LogWarning("DeactivateBudgetPart: Budget part to deactivate and receiving budget part IDs are the same ({Id}).", dto.BudgetPartIdToDeactivate);
                    return BadRequest(new { success = false, message = "La partida a desactivar no puede ser la misma que la partida receptora." });
                }

                // Obtener ambas partidas, incluyendo sus proyectos
                var budgetParts = await _context.BudgetParts
                    .Include(b => b.Project)
                    .Where(b => b.BudgetPartId == dto.BudgetPartIdToDeactivate || b.BudgetPartId == dto.ReceivingBudgetPartId)
                    .ToListAsync();

                var budgetPartToDeactivate = budgetParts.FirstOrDefault(b => b.BudgetPartId == dto.BudgetPartIdToDeactivate);
                var receivingBudgetPart = budgetParts.FirstOrDefault(b => b.BudgetPartId == dto.ReceivingBudgetPartId);

                // Validar existencia de ambas partidas
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

                // Validar que ambas partidas pertenezcan al mismo proyecto
                if (budgetPartToDeactivate.ProjectId != receivingBudgetPart.ProjectId)
                {
                    _logger.LogWarning("DeactivateBudgetPart: Budget parts {DeactivateId} (Project {DeactivateProjId}) and {ReceivingId} (Project {ReceivingProjId}) belong to different projects.",
                        dto.BudgetPartIdToDeactivate, budgetPartToDeactivate.ProjectId, dto.ReceivingBudgetPartId, receivingBudgetPart.ProjectId);
                    return BadRequest(new { success = false, message = "Las partidas a desactivar y receptora deben pertenecer al mismo proyecto." });
                }

                // Validar que el proyecto de ambas partidas esté activo
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

                // Validar que la partida a desactivar esté actualmente activa
                if (budgetPartToDeactivate.Status != "Active")
                {
                    _logger.LogWarning("DeactivateBudgetPart: Budget part with ID {DeactivateId} is already inactive (Status: {Status}).", dto.BudgetPartIdToDeactivate, budgetPartToDeactivate.Status);
                    return BadRequest(new { success = false, message = $"La partida presupuestaria con ID {dto.BudgetPartIdToDeactivate} ya se encuentra inactiva." });
                }

                // --- Realizar la transferencia de fondos y desactivación ---
                decimal fundsToTransfer = budgetPartToDeactivate.RemainingAmount;

                // Transferir fondos a la partida receptora
                receivingBudgetPart.RemainingAmount += fundsToTransfer;

                // Establecer el RemainingAmount de la partida a desactivar en 0
                budgetPartToDeactivate.RemainingAmount = 0;

                // Desactivar la partida
                budgetPartToDeactivate.Status = "Inactive"; // O el string/enum que uses para 'desactivado'

                // Guardar los cambios en la base de datos
                await _context.SaveChangesAsync();

                _logger.LogInformation("DeactivateBudgetPart: Budget part {DeactivateId} deactivated and {Funds} funds transferred to {ReceivingId}.",
                    dto.BudgetPartIdToDeactivate, fundsToTransfer, dto.ReceivingBudgetPartId);

                // Devolver una respuesta exitosa con los datos actualizados
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
        /// Updates the name of an existing budget part.
        /// </summary>
        /// <remarks>
        /// This endpoint allows modifying only the <paramref name="dto.PartName"/> of a budget part identified by its ID.
        /// The budget part must exist and its associated project must be "Active".
        /// Other properties like AllocatedAmount or Status are not modifiable through this endpoint.
        /// </remarks>
        /// <param name="id">The unique identifier of the budget part to update.</param>
        /// <param name="dto">A <see cref="UpdateBudgetPartNameDTO"/> containing the new name for the budget part.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> indicating the outcome of the operation:
        /// <list type="bullet">
        ///   <item><description><see cref="Microsoft.AspNetCore.Mvc.OkObjectResult"/> (HTTP 200 OK) with a success message
        ///   and the updated <see cref="BudgetPartResponseDTO"/> if the update is successful.</description></item>
        ///   <item><description><see cref="Microsoft.AspNetCore.Mvc.BadRequestResult"/> (HTTP 400 Bad Request) with a failure message
        ///   for invalid input DTO.</description></item>
        ///   <item><description><see cref="Microsoft.AspNetCore.Mvc.NotFoundResult"/> (HTTP 404 Not Found) with a failure message
        ///   if the budget part with the given ID is not found, or its associated project is not active.</description></item>
        ///   <item><description><see cref="Microsoft.AspNetCore.Mvc.StatusCodeResult"/> (HTTP 500 Internal Server Error) with a failure message
        ///   for unexpected server errors during the process.</description></item>
        /// </list>
        /// </returns>
        [HttpPut("{id}")] // Using PUT for updating a specific resource
        public async Task<IActionResult> UpdateBudgetPartName(int id, UpdateBudgetPartNameDTO dto) // Method name changed
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("UpdateBudgetPartName: Invalid model state for ID {Id}. Errors: {Errors}", id, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(new { success = false, message = "Datos de actualización inválidos.", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
            }

            try
            {
                // Find the budget part to update, including its project to check status
                var budgetPart = await _context.BudgetParts
                    .Include(b => b.Project)
                    .FirstOrDefaultAsync(b => b.BudgetPartId == id);

                if (budgetPart == null)
                {
                    _logger.LogWarning("UpdateBudgetPartName: Budget part with ID {Id} not found.", id);
                    return NotFound(new { success = false, message = $"Partida presupuestaria con ID {id} no encontrada." });
                }

                // Validate that the associated project is active
                if (budgetPart.Project == null || budgetPart.Project.Status != "Active")
                {
                    _logger.LogWarning("UpdateBudgetPartName: Project of budget part (ID: {ProjectId}) is not active.", budgetPart.ProjectId);
                    return BadRequest(new { success = false, message = "El proyecto asociado a esta partida no está activo. No se puede actualizar el nombre." });
                }

                // --- Apply only the PartName update ---
                budgetPart.PartName = dto.PartName.Trim();

                // Save the changes
                await _context.SaveChangesAsync();

                _logger.LogInformation("UpdateBudgetPartName: Budget part with ID {Id} name updated successfully.", id);

                // Return the updated budget part data
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
