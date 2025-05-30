namespace GPP_API.DTO
{
    public class AuditLogDTO
    {
        public int LogId { get; set; }
        public string ActionType { get; set; } = null!;
        public string? ActionDescription { get; set; }
        public DateTime? ActionDate { get; set; }
    }

}
