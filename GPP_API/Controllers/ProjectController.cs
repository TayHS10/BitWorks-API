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
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectController : ControllerBase
    {
        private readonly GestionPresupuestariaDbContext _context;

        public ProjectController(GestionPresupuestariaDbContext context)
        {
            _context = context;
        }

        // GET: api/Project
        [HttpGet]
        public async Task<IActionResult> GetProjects()
        {
            try
            {
                var projects = await _context.Projects
                    .Include(p => p.Alerts)
                    .Include(p => p.BudgetParts).ThenInclude(b => b.Expenses)
                    .Include(p => p.Expenses)
                    .Include(p => p.ManagerEmailNavigation)
                    .ToListAsync();

                var dtoList = projects.Select(MapToProjectDTO).ToList();

                return Ok(new { success = true, data = dtoList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener proyectos.", detail = ex.Message });
            }
        }

        // GET: api/Project/5
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

        // POST: api/Project
        [HttpPost]
        public async Task<IActionResult> CreateProject(CreateProjectDTO dto)
        {
            try
            {
                var managerExists = await _context.Users.AnyAsync(u => u.Email == dto.ManagerEmail);
                if (!managerExists)
                    return BadRequest(new { success = false, message = "El manager especificado no existe." });

                if (dto.BudgetParts == null || !dto.BudgetParts.Any())
                    return BadRequest(new { success = false, message = "Debe incluir al menos una partida presupuestaria." });

                // Calcular el presupuesto total sumando las partidas
                decimal totalBudget = dto.BudgetParts.Sum(bp => bp.AllocatedAmount);

                var project = new Project
                {
                    ProjectCode = dto.ProjectCode,
                    ProjectName = dto.ProjectName,
                    Description = dto.Description,
                    Budget = totalBudget,
                    RemainingBudget = totalBudget,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    ManagerEmail = dto.ManagerEmail,
                    // Asumiendo que Project tiene navegación para BudgetParts
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

        //// DELETE: api/Project/5
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> DeleteProject(int id)
        //{
        //    try
        //    {
        //        var project = await _context.Projects.FindAsync(id);
        //        if (project == null)
        //            return NotFound(new { success = false, message = $"Proyecto con id {id} no encontrado." });

        //        _context.Projects.Remove(project);
        //        await _context.SaveChangesAsync();

        //        return Ok(new { success = true, message = "Proyecto eliminado correctamente." });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { success = false, message = "Error al eliminar el proyecto.", detail = ex.Message });
        //    }
        //}

        // PUT: api/Project/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProject(int id, UpdateProjectDTO dto)
        {
            try
            {
                var project = await _context.Projects.FindAsync(id);
                if (project == null)
                    return NotFound(new { success = false, message = $"Proyecto con id {id} no encontrado." });

                if (dto.ManagerEmail != null)
                {
                    var managerExists = await _context.Users.AnyAsync(u => u.Email == dto.ManagerEmail);
                    if (!managerExists)
                        return BadRequest(new { success = false, message = "El manager especificado no existe." });

                    project.ManagerEmail = dto.ManagerEmail;
                }

                project.ProjectCode = dto.ProjectCode ?? project.ProjectCode;
                project.ProjectName = dto.ProjectName ?? project.ProjectName;
                project.Description = dto.Description ?? project.Description;

                // Guardar cambios
                await _context.SaveChangesAsync();

                var updatedDto = MapToProjectDTO(project);
                return Ok(new { success = true, message = "Proyecto actualizado correctamente.", data = updatedDto });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error al actualizar el proyecto.", detail = ex.Message });
            }
        }


        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(e => e.ProjectId == id);
        }

        // Mapeo interno a DTO
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
