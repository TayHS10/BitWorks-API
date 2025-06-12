using GPP_API.DTO.User;

namespace GPP_API.Services
{
    public interface IAuthorizationService
    {
        Task<AuthorizationResponse> ReturnToken(LoginUserDTO authorization);
    }
}
