namespace GPP_API.DTO.User
{

    public class CreateUserDTO
    {
        public required string FullName { get; set; } = null!;

        public required string Email { get; set; } = null!;

        public required string Password { get; set; } = null!; // Or just 'public required string Password { get; set; };'

        public required string Role { get; set; } = null!; 
    }
}