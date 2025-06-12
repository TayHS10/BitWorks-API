using System.ComponentModel.DataAnnotations;

namespace GPP_API.DTO.BudgetPart
{
    public class UpdateBudgetPartNameDTO
    {
        [Required(ErrorMessage = "El nombre de la partida es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre de la partida no puede exceder los 100 caracteres.")]
        public string PartName { get; set; } = null!;
    }
}
