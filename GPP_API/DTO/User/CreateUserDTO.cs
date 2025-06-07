namespace GPP_API.DTO.User
{
    /// <summary>
    /// Represents the data transfer object used for creating a new user.
    /// This DTO contains the essential information provided by the client
    /// to register a new user in the system.
    /// </summary>
    public class CreateUserDTO
    {
        /// <summary>
        /// Gets or sets the full name of the user. This field is required.
        /// </summary>
        public required string FullName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the email address of the user. This field is required and must be unique.
        /// </summary>
        public required string Email { get; set; } = null!;

        /// <summary>
        /// Gets or sets the password for the new user. This field is required.
        /// The password will be hashed by the server before storage.
        /// </summary>
        public required string Password { get; set; } = null!; // Or just 'public required string Password { get; set; };'

        /// <summary>
        /// Gets or sets the role to be assigned to the new user. This field is required.
        /// Server-side validation should ensure this is a valid and authorized role.
        /// </summary>
        public required string Role { get; set; } = null!; 
    }
}