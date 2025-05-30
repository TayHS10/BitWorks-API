using GPP_API.Models;

namespace GPP_API.Services
{
    public interface IAuthorizationService
    {
        Task<AuthorizationResponse> ReturnToken(LoginDTO authorization);
    }
}
