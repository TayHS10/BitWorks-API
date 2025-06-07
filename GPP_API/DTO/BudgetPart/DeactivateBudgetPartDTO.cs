using System.ComponentModel.DataAnnotations;

namespace GPP_API.DTO.BudgetPart
{
    public class DeactivateBudgetPartDTO
    {
        [Required(ErrorMessage = "El ID de la partida a desactivar es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID de la partida a desactivar debe ser un número positivo.")]
        public int BudgetPartIdToDeactivate { get; set; }

        [Required(ErrorMessage = "El ID de la partida receptora de fondos es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID de la partida receptora debe ser un número positivo.")]
        public int ReceivingBudgetPartId { get; set; }
    }
}
