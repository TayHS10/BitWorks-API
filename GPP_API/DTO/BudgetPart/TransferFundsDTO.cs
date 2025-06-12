using System.ComponentModel.DataAnnotations;

namespace GPP_API.DTO.BudgetPart
{
    public class TransferFundsDTO
    {
        [Required(ErrorMessage = "El ID de la partida de origen es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID de la partida de origen debe ser un número positivo.")]
        public int SourceBudgetPartId { get; set; }

        [Required(ErrorMessage = "El ID de la partida de destino es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID de la partida de destino debe ser un número positivo.")]
        public int DestinationBudgetPartId { get; set; }

        [Required(ErrorMessage = "El monto a transferir es obligatorio.")]
        [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "El monto a transferir debe ser mayor que cero.")]
        public decimal Amount { get; set; }
    }
}
