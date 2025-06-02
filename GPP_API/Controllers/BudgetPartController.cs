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
        private readonly GestionPresupuestariaDbContext _context;

        public BudgetPartController(GestionPresupuestariaDbContext context)
        {
            _context = context;
        }

        // GET: api/Project
        [HttpGet]
        public async Task<IActionResult> GetBudgeParts()
        {
            try
            {
                // Traemos todas las partidas presupuestarias con sus gastos
                var budgetParts = await _context.BudgetParts
                    .Include(b => b.Expenses)
                    .ToListAsync();

                // Mapeamos a DTOs
                var dtoList = budgetParts.Select(MapToBudgetPartDTO).ToList();

                return Ok(new { success = true, data = dtoList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener las partidas presupuestarias.", detail = ex.Message });
            }
        }

        // GET: api/BudgetPart/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBudgetPartById(int id)
        {
            try
            {
                var budgetPart = await _context.BudgetParts
                    .Include(b => b.Expenses)
                    .FirstOrDefaultAsync(b => b.BudgetPartId == id);

                if (budgetPart == null)
                {
                    return NotFound(new { success = false, message = $"Partida presupuestaria con ID {id} no encontrada." });
                }

                var dto = MapToBudgetPartDTO(budgetPart);

                return Ok(new { success = true, data = dto });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener la partida presupuestaria.", detail = ex.Message });
            }
        }

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
