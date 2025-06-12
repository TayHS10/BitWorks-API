namespace GPP_API.DTO.User
{

    public class ResetPasswordConfirmDTO
    {

        public required string Email { get; set; }

        public required string Token { get; set; }


        public required string NewPassword { get; set; }

   
        public required string ConfirmNewPassword { get; set; }
    }
}
