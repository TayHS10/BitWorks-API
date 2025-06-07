namespace GPP_API.Services
{
    public class AuthorizationResponse
    {
        public string Token { get; set; } = null!;
        public bool Result { get; set; }
        public string Msj { get; set; } = null!;
    }
}
