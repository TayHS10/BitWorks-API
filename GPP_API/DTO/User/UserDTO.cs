namespace GPP_API.DTO.User
{
    public class UserDTO
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; }
        public string Role { get; set; } = null!;
    }

}
