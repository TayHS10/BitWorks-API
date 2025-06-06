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

        public BudgetPartController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todas las partidas presupuestarias junto con sus gastos relacionados.
        /// </summary>
        /// <returns>Lista de partidas presupuestarias con gastos.</returns>
        [HttpGet]
        public async Task<IActionResult> GetBudgeParts()
        {
            try
            {
                // Consulta todas las partidas presupuestarias incluyendo sus gastos (expenses)
                var budgetParts = await _context.BudgetParts
                    .Include(b => b.Expenses)
                    .ToListAsync();

                // Mapea las entidades a DTOs para controlar los datos que se exponen en la API
                var dtoList = budgetParts.Select(MapToBudgetPartDTO).ToList();

                // Retorna resultado con código 200 OK y lista de partidas presupuestarias
                return Ok(new { success = true, data = dtoList });
            }
            catch (Exception ex)
            {
                // En caso de error inesperado, retorna código 500 Internal Server Error con mensaje y detalle del error
                return StatusCode(500, new { success = false, message = "Error al obtener las partidas presupuestarias.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene una partida presupuestaria por su ID, incluyendo sus gastos asociados.
        /// </summary>
        /// <param name="id">ID de la partida presupuestaria.</param>
        /// <returns>Partida presupuestaria solicitada o error si no se encuentra.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBudgetPartById(int id)
        {
            try
            {
                // Busca la partida presupuestaria por ID incluyendo gastos relacionados
                var budgetPart = await _context.BudgetParts
                    .Include(b => b.Expenses)
                    .FirstOrDefaultAsync(b => b.BudgetPartId == id);

                // Verifica si la partida existe
                if (budgetPart == null)
                {
                    // Retorna 404 Not Found si no existe
                    return NotFound(new { success = false, message = $"Partida presupuestaria con ID {id} no encontrada." });
                }

                // Mapea la entidad a DTO para exponer solo la info necesaria
                var dto = MapToBudgetPartDTO(budgetPart);

                // Retorna 200 OK con la partida encontrada
                return Ok(new { success = true, data = dto });
            }
            catch (Exception ex)
            {
                // Retorna error 500 Internal Server Error si ocurre un fallo inesperado
                return StatusCode(500, new { success = false, message = "Error al obtener la partida presupuestaria.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Crea una nueva partida presupuestaria y actualiza el presupuesto total y restante del proyecto asociado.
        /// </summary>
        /// <param name="projectId">ID del proyecto al cual se asigna la partida.</param>
        /// <param name="dto">Datos de la nueva partida presupuestaria.</param>
        /// <returns>Partida creada con sus datos o error específico.</returns>
        [HttpPost("{projectId}")]
        public async Task<IActionResult> CreateBudgetPart(int projectId, CreateBudgetPartDTO dto)
        {
            // Validación inicial de entrada
            if (dto == null)
            {
                return BadRequest(new { success = false, message = "Los datos de la partida presupuestaria son requeridos." });
            }

            try
            {
                // Intenta obtener el proyecto con su lista de partidas para actualización
                var project = await _context.Projects
                    .Include(p => p.BudgetParts)
                    .FirstOrDefaultAsync(p => p.ProjectId == projectId);

                // Verifica que el proyecto exista
                if (project == null)
                {
                    return NotFound(new { success = false, message = $"Proyecto con id {projectId} no encontrado." });
                }

                // Valida que el nombre de la partida no sea vacío o nulo
                if (string.IsNullOrWhiteSpace(dto.PartName))
                {
                    return BadRequest(new { success = false, message = "El nombre de la partida presupuestaria es obligatorio." });
                }

                // Valida que el monto asignado sea mayor a cero
                if (dto.AllocatedAmount <= 0)
                {
                    return BadRequest(new { success = false, message = "El monto asignado debe ser mayor que cero." });
                }

                // Crea la nueva entidad BudgetPart con los datos recibidos
                var newBudgetPart = new BudgetPart
                {
                    ProjectId = projectId,
                    PartName = dto.PartName.Trim(),
                    AllocatedAmount = dto.AllocatedAmount,
                    RemainingAmount = dto.AllocatedAmount,
                    CreatedAt = DateTime.UtcNow,
                    Status = "Active"
                };

                // Agrega la nueva partida al contexto para insertarla en BD
                _context.BudgetParts.Add(newBudgetPart);

                // Actualiza el presupuesto total y el presupuesto restante del proyecto sumando la nueva partida
                project.Budget += dto.AllocatedAmount;
                project.RemainingBudget += dto.AllocatedAmount;

                // Guarda los cambios en la base de datos
                await _context.SaveChangesAsync();

                // Mapea la entidad recién creada a DTO para respuesta
                var resultDto = MapToBudgetPartDTO(newBudgetPart);

                // Retorna código 201 Created con la partida creada y su URI para obtener detalles
                return CreatedAtAction(nameof(GetBudgetPartById), new { id = newBudgetPart.BudgetPartId }, new { success = true, data = resultDto });
            }
            catch (DbUpdateException dbEx)
            {
                // Captura errores relacionados con la base de datos
                return StatusCode(500, new { success = false, message = "Error al guardar la partida presupuestaria en la base de datos.", detail = dbEx.Message });
            }
            catch (Exception ex)
            {
                // Captura cualquier otro error inesperado
                return StatusCode(500, new { success = false, message = "Error inesperado al crear la partida presupuestaria.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Mapea una entidad BudgetPart a un DTO para exponer solo los campos deseados en la API.
        /// </summary>
        /// <param name="b">Entidad BudgetPart.</param>
        /// <returns>DTO con datos de la partida presupuestaria.</returns>
        private static BudgetPartDTO MapToBudgetPartDTO(BudgetPart b) => new()
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
        };
    }
}
