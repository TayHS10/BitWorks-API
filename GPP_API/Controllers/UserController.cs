using GPP_API.Models;
using GPP_API.Services;
using GPP_API.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GPP_API.DTO.User;

namespace GPP_API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly GestionPresupuestariaDbContext _context = null;

        private readonly IAuthorizationService _authorizationServices;

        public UserController(GestionPresupuestariaDbContext pContext,
            IAuthorizationService authorizationServices)
        {
            _context = pContext;

            _authorizationServices = authorizationServices;
        }

        //metodo para devolver todos los usuarios existentes
        [HttpGet("ListUser")]

        public List<User> ListaClientes()
        {

            List<User> list = _context.Users.ToList();

            return list;

        }

        /// <summary>
        /// metodo para crear el usuario utilizando el dto para facilicar la 
        /// entrada de datos
        /// </summary>
        /// <param name="pUser"></param>
        /// <returns>devulve un mensaje diciendo si la operacion fue
        /// excitosa o hubo un error</returns>
        [HttpPost("CreateUser")]

        public async Task<string> CreateUser(UserDTO pUser)
        {

            string msj = "";
            try
            {
                // Validación de objeto nulo
                if (pUser == null)
                {
                    msj = ("Error: Los datos del usuario no deben estar vacíos");
                    return msj;
                }

                // Validar que el email no exista usando FirstOrDefault
               var UserExists = await _context.Users
                    .FirstOrDefaultAsync(c => c.Email == pUser.Email);

                if (UserExists != null)
                {
                    msj = ("Error: Ya existe un usuario registrado con este email");
                    return msj;
                }

                // Mapear DTO a entidad Cliente
                var newUser = new User
                {
                    FullName = pUser.FullName,
                    Email = pUser.Email,
                    Password = pUser.Password,
                    Role = "Accountant",
                    Status = "Active"

                };

                // Agregar y guardar
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                msj = "Usuario creado exitosamente";
            }
            catch (Exception ex)
            {

                msj = ex.InnerException.Message;
            }
            return msj;
        }
        
        //buscar usuario por email
        [HttpGet("SearchForEmail")]

        public User SearchEmail(string pEmail)
        {

            User user = _context.Users.FirstOrDefault(x => x.Email == pEmail);

            return user;
        }

        //buscar usuario por id
        [HttpGet("SearchForID")]

        public User SearchID(int pID)
        {

            User user = _context.Users.FirstOrDefault(i => i.UserId == pID);

            return user;
        }

        //metodo de autenticar a traves del token
        [HttpPost]
        [Route("Authenticate")]
        public async Task<IActionResult> authenticate(LoginDTO authenticated)
        {  
            var autorized = await _authorizationServices.ReturnToken(authenticated);
            
            if (autorized == null)
            {  
                return Unauthorized();
            }
            else
            {  
                return Ok(autorized);
            }
        }
    }
}
