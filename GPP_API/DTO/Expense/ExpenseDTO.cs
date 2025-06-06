namespace GPP_API.DTO.Expense
{
    public class ExpenseDTO
    {
        public int ExpenseId { get; set; }
        public decimal ExpenseAmount { get; set; }
        public DateOnly ExpenseDate { get; set; }
        public string? DocumentReference { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

}
