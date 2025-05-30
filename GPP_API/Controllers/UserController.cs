using GPP_API.Models;
using GPP_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace GPP_API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly GestionPresupuestariaDbContext _contexto = null;

        private readonly IAuthorizationService _authorizationServices;

        public UserController(GestionPresupuestariaDbContext pContext,
            IAuthorizationService authorizationServices)
        {
            _contexto = pContext;

            _authorizationServices = authorizationServices;
        }

        [HttpPost]
        [Route("Authenticate")]
        public async Task<IActionResult> authenticate(LoginDTO authenticated)
        {  
            var autorizado = await _authorizationServices.ReturnToken(authenticated);
            
            if (autorizado == null)
            {  
                return Unauthorized();
            }
            else
            {  
                return Ok(autorizado);
            }
        }
    }
}
