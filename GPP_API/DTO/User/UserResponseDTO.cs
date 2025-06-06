namespace GPP_API.DTO.User
{
    /// <summary>
    /// Represents the data transfer object used for sending user details in API responses.
    /// This DTO contains information about a user that is safe to expose to clients.
    /// </summary>
    public class UserResponseDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier of the user.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the full name of the user.
        /// </summary>
        public string FullName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the email address of the user.
        /// </summary>
        public string Email { get; set; } = null!;

        /// <summary>
        /// Gets or sets the assigned role of the user (e.g., "Accountant", "Administrator").
        /// </summary>
        public string Role { get; set; } = null!;
    }
}