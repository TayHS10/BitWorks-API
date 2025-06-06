namespace GPP_API.DTO.RoleChangeRequest
{
    public class RoleChangeRequestDTO
    {
        public int RequestId { get; set; }
        public string? RequestedRole { get; set; }
        public string? Justification { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

}
