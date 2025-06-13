namespace GPP_API.DTO.RoleChangeRequest
{
    public class CreateRoleChangeRequestDTO
    {
        public required string RequestedRole { get; set; }
        public required string Justification { get; set; }
        public required string EmailAddress { get; set; }
        public required string FullName { get; set; }
    }
}
