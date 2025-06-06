namespace GPP_API.DTO.User
{
    /// <summary>
    /// Represents the data transfer object used for user login requests.
    /// This DTO carries the credentials provided by the client for authentication.
    /// </summary>
    public class LoginUserDTO
    {
        /// <summary>
        /// Gets or sets the email address of the user attempting to log in. This field is required.
        /// </summary>
        public string Email { get; set; } = null!;

        /// <summary>
        /// Gets or sets the plain-text password provided by the user for authentication. This field is required.
        /// The password will be verified against a stored hash on the server.
        /// </summary>
        public string Password { get; set; } = null!;
    }
}