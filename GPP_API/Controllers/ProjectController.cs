// Importación de bibliotecas necesarias
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GPP_API.Models;
using GPP_API.DTO.Project;
using GPP_API.DTO.BudgetPart;
using GPP_API.DTO.Alert;
using GPP_API.DTO.User;
using GPP_API.DTO.Expense;
using GPP_API.DTO;

namespace GPP_API.Controllers
{
    // Marca la clase como controlador de API y define su ruta base
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectController : ControllerBase
    {
        private readonly GestionPresupuestariaDbContext _context;

        // Constructor que recibe el contexto de base de datos por inyección de dependencias
        public ProjectController(GestionPresupuestariaDbContext context)
        {
            _context = context;
        }



        // =========================
        // GET: api/Project
        // Obtiene todos los proyectos con sus relaciones (alertas, partidas, gastos, y manager)
        // =========================
        [HttpGet]
        public async Task<IActionResult> GetProjects()
        {
            try
            {
                // Consulta todos los proyectos con sus datos relacionados
                var projects = await _context.Projects
                    .Include(p => p.Alerts)
                    .Include(p => p.BudgetParts).ThenInclude(b => b.Expenses)
                    .Include(p => p.Expenses)
                    .Include(p => p.ManagerEmailNavigation)
                    .ToListAsync();

                // Convierte los resultados a DTOs
                var dtoList = projects.Select(MapToProjectDTO).ToList();

                return Ok(new { success = true, data = dtoList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener proyectos.", detail = ex.Message });
            }
        }

        // =========================
        // GET: api/Project/active
        // Obtiene solo los proyectos con estado "Active"
        // =========================
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveProjects()
        {
            try
            {
                var activeProjects = await _context.Projects
                    .Where(p => p.Status == "Active")
                    .Include(p => p.Alerts)
                    .Include(p => p.BudgetParts).ThenInclude(b => b.Expenses)
                    .Include(p => p.Expenses)
                    .Include(p => p.ManagerEmailNavigation)
                    .ToListAsync();

                var dtoList = activeProjects.Select(MapToProjectDTO).ToList();

                return Ok(new { success = true, data = dtoList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener proyectos activos.", detail = ex.Message });
            }
        }

        // =========================
        // GET: api/Project/{id}
        // Obtiene un proyecto por su ID
        // =========================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProject(int id)
        {
            try
            {
                var project = await _context.Projects
                    .Include(p => p.Alerts)
                    .Include(p => p.BudgetParts).ThenInclude(b => b.Expenses)
                    .Include(p => p.Expenses)
                    .Include(p => p.ManagerEmailNavigation)
                    .FirstOrDefaultAsync(p => p.ProjectId == id);

                if (project == null)
                    return NotFound(new { success = false, message = $"Proyecto con id {id} no encontrado." });

                var dto = MapToProjectDTO(project);
                return Ok(new { success = true, data = dto });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener el proyecto.", detail = ex.Message });
            }
        }

        // =========================
        // POST: api/Project
        // Crea un nuevo proyecto con sus partidas presupuestarias
        // =========================
        [HttpPost]
        public async Task<IActionResult> CreateProject(CreateProjectDTO dto)
        {
            try
            {
                // Verifica que el manager exista
                var managerExists = await _context.Users.AnyAsync(u => u.Email == dto.ManagerEmail);
                if (!managerExists)
                    return BadRequest(new { success = false, message = "El manager especificado no existe." });

                // Asegura que exista al menos una partida presupuestaria
                if (dto.BudgetParts == null || !dto.BudgetParts.Any())
                    return BadRequest(new { success = false, message = "Debe incluir al menos una partida presupuestaria." });

                // Calcula el presupuesto total sumando las partidas
                decimal totalBudget = dto.BudgetParts.Sum(bp => bp.AllocatedAmount);

                // Crea el objeto Project con sus relaciones
                var project = new Project
                {
                    ProjectCode = dto.ProjectCode,
                    ProjectName = dto.ProjectName,
                    Description = dto.Description,
                    Budget = totalBudget,
                    RemainingBudget = totalBudget,
                    Status = "Active", // Siempre inicia como activo
                    CreatedAt = DateTime.UtcNow,
                    ManagerEmail = dto.ManagerEmail,
                    BudgetParts = dto.BudgetParts.Select(bp => new BudgetPart
                    {
                        PartName = bp.PartName,
                        AllocatedAmount = bp.AllocatedAmount,
                        RemainingAmount = bp.AllocatedAmount,
                        CreatedAt = DateTime.UtcNow
                    }).ToList()
                };

                _context.Projects.Add(project);
                await _context.SaveChangesAsync();

                var resultDto = MapToProjectDTO(project);

                // Devuelve respuesta con ruta al proyecto recién creado
                return CreatedAtAction(nameof(GetProject), new { id = project.ProjectId }, new { success = true, data = resultDto });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new { success = false, message = "Error al guardar el proyecto en la base de datos.", detail = dbEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error inesperado al crear el proyecto.", detail = ex.Message });
            }
        }

        // =========================
        // DELETE: api/Project/{id}
        // Desactiva un proyecto cambiando su estado a "Inactive"
        // =========================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            try
            {
                var project = await _context.Projects.FindAsync(id);
                if (project == null)
                    return NotFound(new { success = false, message = $"Proyecto con id {id} no encontrado." });

                // No se elimina de la base de datos, solo se cambia el estado
                project.Status = "Inactive";
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Proyecto desactivado correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error al desactivar el proyecto.", detail = ex.Message });
            }
        }

        // =========================
        // PUT: api/Project/{id}
        // Actualiza los datos de un proyecto existente
        // =========================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProject(int id, UpdateProjectDTO dto)
        {
            try
            {
                var project = await _context.Projects.FindAsync(id);
                if (project == null)
                    return NotFound(new { success = false, message = $"Proyecto con id {id} no encontrado." });

                // Si se envía un nuevo manager, se valida que exista
                if (dto.ManagerEmail != null)
                {
                    var managerExists = await _context.Users.AnyAsync(u => u.Email == dto.ManagerEmail);
                    if (!managerExists)
                        return BadRequest(new { success = false, message = "El manager especificado no existe." });

                    project.ManagerEmail = dto.ManagerEmail;
                }

                // Actualiza solo los campos enviados
                project.ProjectCode = dto.ProjectCode ?? project.ProjectCode;
                project.ProjectName = dto.ProjectName ?? project.ProjectName;
                project.Description = dto.Description ?? project.Description;

                await _context.SaveChangesAsync();

                var updatedDto = MapToProjectDTO(project);
                return Ok(new { success = true, message = "Proyecto actualizado correctamente.", data = updatedDto });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error al actualizar el proyecto.", detail = ex.Message });
            }
        }

        // =========================
        // Método privado para verificar si existe un proyecto con cierto ID
        // =========================
        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(e => e.ProjectId == id);
        }

        // =========================
        // Método para mapear de Project (modelo) a ProjectDTO
        // Incluye alertas, partidas presupuestarias, gastos y manager
        // =========================
        private static ProjectDTO MapToProjectDTO(Project p) => new()
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
            Manager = p.ManagerEmailNavigation == null ? null : new UserDTO
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
            BudgetParts = p.BudgetParts.Select(b => new BudgetPartDTO
            {
                BudgetPartId = b.BudgetPartId,
                PartName = b.PartName,
                AllocatedAmount = b.AllocatedAmount,
                RemainingAmount = b.RemainingAmount,
                CreatedAt = b.CreatedAt,
                Expenses = b.Expenses.Select(e => new ExpenseDTO
                {
                    ExpenseId = e.ExpenseId,
                    ExpenseAmount = e.ExpenseAmount,
                    ExpenseDate = e.ExpenseDate,
                    DocumentReference = e.DocumentReference,
                    Description = e.Description,
                    CreatedAt = e.CreatedAt
                }).ToList()
            }).ToList(),
            Expenses = p.Expenses.Select(e => new ExpenseDTO
            {
                ExpenseId = e.ExpenseId,
                ExpenseAmount = e.ExpenseAmount,
                ExpenseDate = e.ExpenseDate,
                DocumentReference = e.DocumentReference,
                Description = e.Description,
                CreatedAt = e.CreatedAt
            }).ToList()
        };
    }
}
