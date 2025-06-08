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
    // Marca la clase como controlador de API y define su ruta base
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<ProjectController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectController"/> class.
        /// </summary>
        /// <param name="context">The database context, typically injected via dependency injection,
        /// used for interacting with the application's data store.</param>
        /// <param name="emailService">The email service, injected via dependency injection,
        /// used for sending emails related to project operations.</param>
        /// <param name="logger">The logger instance, injected via dependency injection,
        /// used for logging messages and events originating from the ProjectController.</param>
        /// <remarks>
        /// This constructor demonstrates the use of dependency injection to provide necessary services
        /// (<see cref="ApplicationDbContext"/>, <see cref="IEmailService"/>, <see cref="ILogger{TCategoryName}"/>)
        /// to the controller, promoting loose coupling and testability.
        /// </remarks>
        public ProjectController(ApplicationDbContext context, IEmailService emailService, ILogger<ProjectController> logger)
        {
            // Assign the injected ApplicationDbContext to a private field for database operations.
            _context = context;
            // Assign the injected IEmailService to a private field for email functionalities.
            _emailService = emailService;
            // Assign the injected ILogger to a private field for logging purposes.
            _logger = logger;
        }

        /// <summary>
        /// Asynchronously retrieves all projects that have an "Active" status from the database.
        /// </summary>
        /// <remarks>
        /// This endpoint filters projects by their 'Status' property, ensuring only active projects are returned.
        /// It eagerly loads related entities such as Alerts, BudgetParts (including their Expenses),
        /// and ManagerEmailNavigation to provide a comprehensive view of active projects.
        /// The retrieved entities are mapped to a DTO (Data Transfer Object) for client consumption.
        /// </remarks>
        /// <response code="200">Returns an object containing a success flag and a list of active projects.</response>
        /// <response code="500">Returns an object indicating an internal server error along with an error message and details.</response>
        // GET: api/Project/active
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveProjects()
        {
            try
            {
                // Query the database for projects where the Status is "Active".
                // Eagerly load related entities to avoid N+1 query problems and provide complete data.
                var activeProjects = await _context.Projects
                    .Where(p => p.Status == "Active") // Filters projects to include only those with "Active" status.
                    .Include(p => p.Alerts) // Includes associated Alert entities.
                    .Include(p => p.BudgetParts).ThenInclude(b => b.Expenses) // Includes BudgetParts and their nested Expenses.
                    .Include(p => p.ManagerEmailNavigation) // Includes the navigation property for the project manager's email.
                    .ToListAsync(); // Executes the query asynchronously and returns the results as a list.

                // Map the retrieved Project entities to ProjectResponseDTO objects.
                // This practice separates the database model from the API's public contract.
                var dtoList = activeProjects.Select(MapToProjectResponseDTO).ToList();

                // Return a 200 OK response with the success status and the list of DTOs.
                return Ok(new { success = true, data = dtoList });
            }
            catch (Exception ex)
            {
                // Log the exception for debugging and monitoring purposes. (Assuming _logger is available and used here)
                // _logger.LogError(ex, "Error occurred while retrieving active projects.");

                // Return a 500 Internal Server Error response with a failure status and error details.
                return StatusCode(500, new { success = false, message = "Error al obtener proyectos activos.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Asynchronously retrieves a single project by its unique identifier, ensuring it has an "Active" status.
        /// </summary>
        /// <param name="id">The unique integer identifier of the project to retrieve.</param>
        /// <remarks>
        /// This endpoint fetches a specific project from the database using its ID.
        /// It includes a crucial filter to ensure that only projects with a "Status" of "Active" are returned.
        /// Related entities such as Alerts, BudgetParts (including their Expenses), and ManagerEmailNavigation
        /// are eagerly loaded to provide a complete dataset for the requested project.
        /// The retrieved project entity is mapped to a DTO (Data Transfer Object) before being sent as a response.
        /// </remarks>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// The task result contains an <see cref="IActionResult"/>:
        /// <list type="bullet">
        ///     <item><description><see cref="Microsoft.AspNetCore.Mvc.OkObjectResult"/> (HTTP 200 OK) with a success flag and the project DTO if found.</description></item>
        ///     <item><description><see cref="Microsoft.AspNetCore.Mvc.NotFoundObjectResult"/> (HTTP 404 Not Found) with a failure flag and a message if the project with the given ID and active status is not found.</description></item>
        ///     <item><description><see cref="Microsoft.AspNetCore.Mvc.StatusCodeResult"/> (HTTP 500 Internal Server Error) with a failure flag and an error message if an unexpected error occurs.</description></item>
        /// </list>
        /// </returns>
        // GET: api/Project/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProject(int id)
        {
            try
            {
                // Query the database for a specific project by its ID, ensuring it is also "Active".
                // Eagerly load all necessary related entities to provide a complete project object.
                var project = await _context.Projects
                    .Include(p => p.Alerts) // Includes associated Alert entities.
                    .Include(p => p.BudgetParts).ThenInclude(b => b.Expenses) // Includes BudgetParts and their nested Expenses.
                    .Include(p => p.ManagerEmailNavigation) // Includes the navigation property for the project manager's email.
                    .FirstOrDefaultAsync(p => p.ProjectId == id && p.Status == "Active"); // Filters by ProjectId and ensures the Status is "Active".

                // Check if the project was found based on the provided ID and "Active" status.
                if (project == null)
                {
                    // Return a 404 Not Found response if the project is not found.
                    // The message is generic to cover both ID and status not matching.
                    return NotFound(new { success = false, message = $"Project with ID {id} and 'Active' status not found." });
                }

                // Map the retrieved Project entity to a ProjectResponseDTO.
                // This ensures that only relevant data is exposed through the API.
                var dto = MapToProjectResponseDTO(project);

                // Return a 200 OK response with the success status and the project DTO.
                return Ok(new { success = true, data = dto });
            }
            catch (Exception ex)
            {
                // Log the exception for debugging and monitoring purposes.
                // _logger.LogError(ex, "An error occurred while retrieving project with ID {ProjectId}.", id);

                // Return a 500 Internal Server Error response with a failure status and error details.
                return StatusCode(500, new { success = false, message = "Error al obtener el proyecto.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Asynchronously creates a new project based on the provided data transfer object.
        /// </summary>
        /// <param name="dto">The data transfer object containing the details for the new project,
        /// including project code, name, description, budget, manager email, and budget parts.</param>
        /// <remarks>
        /// This endpoint performs several validations before creating a project:
        /// <list type="bullet">
        ///     <item>Ensures the specified project manager email corresponds to an existing user.</item>
        ///     <item>Validates that the project budget is a positive value.</item>
        ///     <item>Checks for the presence of at least one budget part.</item>
        ///     <item>Verifies that the sum of all budget parts' allocated amounts matches the total project budget.</item>
        /// </list>
        /// Upon successful creation, the project's status is set to "Active" and it's saved to the database.
        /// An email confirmation is attempted to be sent to the project manager.
        /// </remarks>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// The task result contains an <see cref="IActionResult"/>:
        /// <list type="bullet">
        ///     <item><description><see cref="Microsoft.AspNetCore.Mvc.CreatedAtActionResult"/> (HTTP 201 Created) with a success flag,
        ///     a message indicating project creation and email sending status, and the created project's DTO.</description></item>
        ///     <item><description><see cref="Microsoft.AspNetCore.Mvc.BadRequestObjectResult"/> (HTTP 400 Bad Request) with a failure flag
        ///     and an error message if any validation fails.</description></item>
        ///     <item><description><see cref="Microsoft.AspNetCore.Mvc.StatusCodeResult"/> (HTTP 500 Internal Server Error) with a failure flag
        ///     and an error message if a database error or unexpected exception occurs during project creation.</description></item>
        /// </list>
        /// </returns>
        // POST: api/Project
        [HttpPost]
        public async Task<IActionResult> CreateProject(CreateProjectDTO dto)
        {
            try
            {
                // Validate that the specified manager email exists in the Users table.
                // This prevents creating projects assigned to non-existent managers.
                var managerExists = await _context.Users.AnyAsync(u => u.Email == dto.ManagerEmail);
                if (!managerExists)
                    return BadRequest(new { success = false, message = "The specified manager does not exist." });

                // NEW VALIDATION: Ensure the project budget is greater than zero.
                // A project must have a valid budget allocated to it.
                if (dto.Budget <= 0)
                {
                    return BadRequest(new { success = false, message = "The project budget must be greater than zero." });
                }

                // Ensure there is at least one budget part defined for the project.
                // Projects without budget parts are considered incomplete.
                if (dto.BudgetParts == null || !dto.BudgetParts.Any())
                    return BadRequest(new { success = false, message = "You must include at least one budget part." });

                // === VERIFICATION: Sum of budget parts equals the project's total budget ===
                // This also implies that the sum of parts must be a positive value.
                // The `VerifyBudgetPartsTotal` method (assumed to be implemented elsewhere) performs this check.
                if (!VerifyBudgetPartsTotal(dto))
                {
                    return BadRequest(new { success = false, message = "The sum of the budget part amounts does not match the total project budget." });
                }

                // Create the Project entity along with its associated BudgetParts.
                // Initialize relevant properties like Status and creation timestamps.
                var project = new Project
                {
                    ProjectCode = dto.ProjectCode,
                    ProjectName = dto.ProjectName,
                    Description = dto.Description,
                    Budget = dto.Budget,
                    RemainingBudget = dto.Budget, // Initially, remaining budget is equal to the total budget.
                    Status = "Active", // Projects always start with an "Active" status.
                    CreatedAt = DateTime.UtcNow, // Record the creation timestamp in UTC.
                    ManagerEmail = dto.ManagerEmail,
                    BudgetParts = dto.BudgetParts.Select(bp => new BudgetPart
                    {
                        PartName = bp.PartName,
                        AllocatedAmount = bp.AllocatedAmount,
                        RemainingAmount = bp.AllocatedAmount, // Initially, remaining amount for each part is its allocated amount.
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow
                    }).ToList() // Convert the selected DTOs into BudgetPart entities.
                };

                // Add the newly created project to the database context.
                _context.Projects.Add(project);
                // Persist the changes to the database asynchronously.
                await _context.SaveChangesAsync();

                // Map the newly created Project entity to a response DTO for the client.
                var resultDto = MapToProjectResponseDTO(project);

                // === Email sending using SendProjectCreatedEmail ===
                try
                {
                    // Send a confirmation email to the project manager.
                    // This service call is independent of the project's successful database persistence.
                    await _emailService.SendProjectCreatedEmail(resultDto.ManagerEmail, resultDto);

                    // Log a successful email delivery for monitoring and auditing.
                    _logger.LogInformation($"Confirmation email successfully sent for project {resultDto.ProjectCode} to manager {resultDto.ManagerEmail}.");

                    // Return a 201 CreatedAtAction response, including the location of the newly created resource
                    // and confirmation of both project creation and email delivery.
                    return CreatedAtAction(nameof(GetProject), new { id = project.ProjectId },
                        new { success = true, message = "Project created and confirmation email sent successfully.", data = resultDto });
                }
                catch (Exception emailEx)
                {
                    // Log any errors encountered during email sending.
                    // This is crucial for troubleshooting email delivery issues without affecting project creation.
                    _logger.LogError(emailEx, $"Error sending confirmation email for project {resultDto.ProjectCode}.");

                    // If email sending fails, the project creation is still considered successful.
                    // A warning message about the email failure is included in the response.
                    return CreatedAtAction(nameof(GetProject), new { id = project.ProjectId },
                        new { success = true, message = "Project created successfully, but there was an error sending the confirmation email.", emailError = emailEx.Message, data = resultDto });
                }

            }
            catch (DbUpdateException dbEx)
            {
                // Catch specific database update exceptions to provide more relevant error messages.
                _logger.LogError(dbEx, "Database error occurred while creating the project.");
                return StatusCode(500, new { success = false, message = "Error saving the project to the database.", detail = dbEx.Message });
            }
            catch (Exception ex)
            {
                // Catch any other unexpected exceptions during the project creation process.
                _logger.LogError(ex, "An unexpected error occurred while creating the project.");
                return StatusCode(500, new { success = false, message = "An unexpected error occurred while creating the project.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Verifies that the sum of allocated amounts in all budget parts matches the total project budget.
        /// </summary>
        /// <param name="dto">The <see cref="CreateProjectDTO"/> containing the project's total budget
        /// and its list of budget parts with allocated amounts.</param>
        /// <returns>
        /// <see langword="true"/> if the sum of all budget part allocated amounts is exactly equal to the project's total budget;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// This method is a crucial validation step during project creation to ensure financial consistency.
        /// It prevents discrepancies between the overall project budget and the sum of its detailed budgetary allocations.
        /// It assumes that `dto.BudgetParts` is not null and has at least one element, as typically validated prior to this call.
        /// </remarks>
        private bool VerifyBudgetPartsTotal(CreateProjectDTO dto)
        {
            // Calculate the sum of the 'AllocatedAmount' property for all budget parts within the DTO.
            // This aggregates the individual budget allocations.
            decimal sumOfAllocatedAmounts = dto.BudgetParts.Sum(bp => bp.AllocatedAmount);

            // Compare the calculated sum of allocated amounts with the project's total budget.
            // Returns true if they are equal, indicating that the budget is correctly distributed among its parts.
            return sumOfAllocatedAmounts == dto.Budget;
        }


        /// <summary>
        /// Asynchronously deactivates a project by changing its status to "Inactive" instead of permanently deleting it.
        /// </summary>
        /// <param name="id">The unique integer identifier of the project to deactivate.</param>
        /// <remarks>
        /// This endpoint implements a "soft delete" mechanism. Instead of removing the project record
        /// from the database, it updates the project's <see cref="Project.Status"/> property to "Inactive".
        /// This preserves historical data and allows for potential re-activation if needed.
        /// </remarks>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// The task result contains an <see cref="IActionResult"/>:
        /// <list type="bullet">
        ///     <item><description><see cref="Microsoft.AspNetCore.Mvc.OkObjectResult"/> (HTTP 200 OK) with a success flag and a confirmation message if the project is successfully deactivated.</description></item>
        ///     <item><description><see cref="Microsoft.AspNetCore.Mvc.NotFoundObjectResult"/> (HTTP 404 Not Found) with a failure flag and a message if the project with the given ID is not found.</description></item>
        ///     <item><description><see cref="Microsoft.AspNetCore.Mvc.StatusCodeResult"/> (HTTP 500 Internal Server Error) with a failure flag and an error message if an unexpected error occurs during deactivation.</description></item>
        /// </list>
        /// </returns>
        // DELETE: api/Project/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            try
            {
                // Attempt to find the project by its ID in the database.
                // FindAsync is efficient for retrieving an entity by its primary key.
                var project = await _context.Projects.FindAsync(id);

                // If no project is found with the given ID, return a 404 Not Found response.
                if (project == null)
                    return NotFound(new { success = false, message = $"Project with ID {id} not found." });

                // IMPORTANT: The project is NOT deleted from the database.
                // Instead, its status is changed to "Inactive" as part of a soft-delete strategy.
                project.Status = "Inactive";

                // Save the changes to the database asynchronously.
                await _context.SaveChangesAsync();

                // Return a 200 OK response indicating successful deactivation.
                return Ok(new { success = true, message = "Project deactivated successfully." });
            }
            catch (Exception ex)
            {
                // Log the exception for debugging and monitoring purposes.
                // _logger.LogError(ex, "An error occurred while deactivating project with ID {ProjectId}.", id);

                // Return a 500 Internal Server Error response with a failure status and error details.
                return StatusCode(500, new { success = false, message = "Error deactivating the project.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Asynchronously updates an existing project identified by its unique ID with the provided data.
        /// </summary>
        /// <param name="id">The unique integer identifier of the project to update.</param>
        /// <param name="dto">The data transfer object containing the updated project details.
        /// Only non-null fields in the DTO will update the corresponding project properties.</param>
        /// <remarks>
        /// This endpoint allows for partial updates of a project. It first retrieves the existing project.
        /// If a new manager email is provided in the DTO, it validates its existence in the user database.
        /// Only the fields explicitly provided (non-null) in the <paramref name="dto"/> will overwrite
        /// the current values of the project.
        /// </remarks>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// The task result contains an <see cref="IActionResult"/>:
        /// <list type="bullet">
        ///     <item><description><see cref="Microsoft.AspNetCore.Mvc.OkObjectResult"/> (HTTP 200 OK) with a success flag,
        ///     a confirmation message, and the updated project's DTO.</description></item>
        ///     <item><description><see cref="Microsoft.AspNetCore.Mvc.NotFoundObjectResult"/> (HTTP 404 Not Found) with a failure flag
        ///     and a message if the project with the given ID is not found.</description></item>
        ///     <item><description><see cref="Microsoft.AspNetCore.Mvc.BadRequestObjectResult"/> (HTTP 400 Bad Request) with a failure flag
        ///     and an error message if the specified new manager email does not exist.</description></item>
        ///     <item><description><see cref="Microsoft.AspNetCore.Mvc.StatusCodeResult"/> (HTTP 500 Internal Server Error) with a failure flag
        ///     and an error message if an unexpected error occurs during the update process.</description></item>
        /// </list>
        /// </returns>
        // PUT: api/Project/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProject(int id, UpdateProjectDTO dto)
        {
            try
            {
                // Attempt to find the project by its ID. FindAsync is efficient for primary key lookups.
                var project = await _context.Projects.FindAsync(id);

                // If the project is not found, return a 404 Not Found response.
                if (project == null)
                    return NotFound(new { success = false, message = $"Project with ID {id} not found." });

                // If a new ManagerEmail is provided in the DTO, validate its existence.
                if (dto.ManagerEmail != null)
                {
                    // Check if the provided manager email corresponds to an existing user.
                    var managerExists = await _context.Users.AnyAsync(u => u.Email == dto.ManagerEmail);
                    if (!managerExists)
                        return BadRequest(new { success = false, message = "The specified manager does not exist." });

                    // Update the project's manager email with the new valid email.
                    project.ManagerEmail = dto.ManagerEmail;
                }

                // Update only the fields that are provided (not null) in the DTO.
                // This allows for partial updates without requiring all fields.
                project.ProjectCode = dto.ProjectCode ?? project.ProjectCode; // If DTO's ProjectCode is null, keep existing.
                project.ProjectName = dto.ProjectName ?? project.ProjectName; // If DTO's ProjectName is null, keep existing.
                project.Description = dto.Description ?? project.Description; // If DTO's Description is null, keep existing.

                // Save the changes made to the project entity back to the database.
                await _context.SaveChangesAsync();

                // Map the updated Project entity back to a response DTO for the client.
                var updatedDto = MapToProjectResponseDTO(project);

                // Return a 200 OK response with the success status, a confirmation message, and the updated DTO.
                return Ok(new { success = true, message = "Project updated successfully.", data = updatedDto });
            }
            catch (Exception ex)
            {
                // Log the exception for debugging and monitoring purposes.
                // _logger.LogError(ex, "An error occurred while updating project with ID {ProjectId}.", id);

                // Return a 500 Internal Server Error response with a failure status and error details.
                return StatusCode(500, new { success = false, message = "Error updating the project.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Maps a <see cref="Project"/> entity object to a <see cref="ProjectResponseDTO"/> data transfer object.
        /// </summary>
        /// <param name="p">The <see cref="Project"/> entity to be mapped.</param>
        /// <returns>
        /// A new <see cref="ProjectResponseDTO"/> instance populated with data from the provided <see cref="Project"/> entity,
        /// including nested DTOs for its related Manager, Alerts, and BudgetParts (along with their Expenses).
        /// </returns>
        /// <remarks>
        /// This static mapping method is crucial for **separating the internal data model (ORM entities) from the external API contract**.
        /// It ensures that only necessary and appropriately structured data is exposed to clients, enhancing security and flexibility.
        /// The method handles nullability for the `ManagerEmailNavigation` property gracefully, preventing potential `NullReferenceException`s.
        /// It performs a **deep mapping**, converting nested collections (Alerts, BudgetParts, Expenses) into their respective DTO representations.
        /// </remarks>
        private static ProjectResponseDTO MapToProjectResponseDTO(Project p) => new()
        {
            // Map basic project properties directly.
            ProjectId = p.ProjectId,
            ProjectCode = p.ProjectCode,
            ProjectName = p.ProjectName,
            Description = p.Description,
            Budget = p.Budget,
            RemainingBudget = p.RemainingBudget,
            CreatedAt = p.CreatedAt,
            ManagerEmail = p.ManagerEmail,
            Status = p.Status,

            // Map the associated Manager (User) entity to a UserResponseDTO.
            // Handles cases where ManagerEmailNavigation might be null if not eagerly loaded or not set.
            Manager = p.ManagerEmailNavigation == null ? null : new UserResponseDTO
            {
                UserId = p.ManagerEmailNavigation.UserId,
                FullName = p.ManagerEmailNavigation.FullName,
                Email = p.ManagerEmailNavigation.Email,
                Role = p.ManagerEmailNavigation.Role
            },

            // Map the collection of Alert entities to a list of AlertDTOs.
            // Each Alert entity is transformed into its DTO representation.
            Alerts = p.Alerts.Select(a => new AlertDTO
            {
                AlertId = a.AlertId,
                AlertType = a.AlertType,
                Message = a.Message,
                AlertDate = a.AlertDate
            }).ToList(),

            // Map the collection of BudgetPart entities to a list of BudgetPartDTOs.
            // This is a nested mapping, as each BudgetPartDTO also contains its Expenses.
            BudgetParts = p.BudgetParts.Select(b => new BudgetPartResponseDTO
            {
                BudgetPartId = b.BudgetPartId,
                PartName = b.PartName,
                AllocatedAmount = b.AllocatedAmount,
                RemainingAmount = b.RemainingAmount,
                CreatedAt = b.CreatedAt,

                // Map the nested collection of Expense entities within each BudgetPart to a list of ExpenseDTOs.
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
        /// Desactiva un proyecto existente, sus partidas presupuestarias asociadas y todos los gastos relacionados.
        /// </summary>
        // ... (comentarios y definición del método) ...
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

                // 3. Desactivar el Proyecto
                project.Status = "Inactive";
                _logger.LogInformation("Deactivating Project ID {Id} (Name: {Name}).", project.ProjectId, project.ProjectName);

                // 4. Desactivar todas las Partidas Presupuestarias del proyecto
                var deactivatedBudgetParts = new List<BudgetPartResponseDTO>();
                var deactivatedExpensesInBudgetParts = new List<ExpenseResponseDTO>(); // Para recolectar todos los gastos desactivados

                foreach (var budgetPart in project.BudgetParts)
                {
                    if (budgetPart.Status == "Active") // Solo desactiva si está activo
                    {
                        budgetPart.Status = "Inactive";
                        _logger.LogInformation("  Deactivating BudgetPart ID {BpId} (Name: {BpName}).", budgetPart.BudgetPartId, budgetPart.PartName);
                        // === CAMBIO AQUÍ ===
                        deactivatedBudgetParts.Add(MapToBudgetPartDTO(budgetPart)); // Usar el mapeador de BudgetPart
                                                                                    // ===================
                    }

                    // 5. Desactivar todos los Gastos asociados a cada Partida
                    foreach (var expense in budgetPart.Expenses)
                    {
                        if (expense.Status == "Active") // Solo desactiva si está activo
                        {
                            expense.Status = "Inactive";
                            _logger.LogInformation("    Deactivating Expense ID {ExpId} (Amount: {Amount}).", expense.ExpenseId, expense.ExpenseAmount);
                            deactivatedExpensesInBudgetParts.Add(new ExpenseResponseDTO
                            {
                                ExpenseId = expense.ExpenseId,
                                ExpenseAmount = expense.ExpenseAmount,
                                ExpenseDate = expense.ExpenseDate, // Incluir otros campos si es necesario en el DTO de respuesta
                                DocumentReference = expense.DocumentReference,
                                Description = expense.Description,
                                CreatedAt = expense.CreatedAt,
                                Status = expense.Status
                            });
                        }
                    }
                }

                // 6. Guardar todos los cambios en la base de datos en una única transacción
                await _context.SaveChangesAsync();

                _logger.LogInformation("DeactivateProject: Project ID {Id} and all associated budget parts/expenses successfully deactivated.", id);

                // 7. Retornar una respuesta exitosa
                return Ok(new
                {
                    success = true,
                    message = $"Proyecto '{project.ProjectName}' (ID: {project.ProjectId}) y sus partidas/gastos asociados desactivados exitosamente.",
                    project = MapToProjectResponseDTO(project),
                    affectedBudgetParts = deactivatedBudgetParts, // Ahora esta lista es correcta
                    affectedExpenses = deactivatedExpensesInBudgetParts // Opcional: devolver los gastos afectados también
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
        /// Maps a <see cref="BudgetPart"/> entity object to a <see cref="BudgetPartResponseDTO"/> data transfer object.
        /// </summary>
        /// <param name="b">The <see cref="BudgetPart"/> entity to be mapped.</param>
        /// <returns>
        /// A new <see cref="BudgetPartResponseDTO"/> instance populated with data from the provided <see cref="BudgetPart"/> entity,
        /// including a nested list of <see cref="ExpenseResponseDTO"/> for its associated expenses.
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
            Expenses = b.Expenses.Select(e => new ExpenseResponseDTO
            {
                ExpenseId = e.ExpenseId,
                ExpenseAmount = e.ExpenseAmount,
                ExpenseDate = e.ExpenseDate,
                DocumentReference = e.DocumentReference,
                Description = e.Description,
                CreatedAt = e.CreatedAt
            }).ToList() // Convert the resulting enumerable of ExpenseDTOs into a List.
        };
    }
}
