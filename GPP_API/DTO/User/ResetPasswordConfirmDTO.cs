namespace GPP_API.DTO.User
{
    /// <summary>
    /// Represents the data transfer object used for confirming a password reset.
    /// This DTO contains the email, the reset token received, and the new password.
    /// </summary>
    public class ResetPasswordConfirmDTO
    {
        /// <summary>
        /// Gets or sets the email address of the user whose password is being reset. This field is required.
        /// </summary>
        public required string Email { get; set; }

        /// <summary>
        /// Gets or sets the unique password reset token. This field is required and is typically sent via email.
        /// </summary>
        public required string Token { get; set; }

        /// <summary>
        /// Gets or sets the new password for the user account. This field is required.
        /// </summary>
        public required string NewPassword { get; set; }

        /// <summary>
        /// Gets or sets the confirmation of the new password. This field is required and must match NewPassword.
        /// </summary>
        public required string ConfirmNewPassword { get; set; }
    }
}
