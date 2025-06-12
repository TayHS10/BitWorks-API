using GPP_API.Models;
using GPP_API.Services;
using GPP_API.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GPP_API.DTO.User;

namespace GPP_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthorizationService _authorizationServices;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="User Controller"/> con el contexto de la base de datos y el servicio de autorización.
        /// </summary>
        /// <param name="context">El contexto de la base de datos utilizado para acceder a los datos de la aplicación.</param>
        /// <param name="authorizationServices">El servicio de autorización utilizado para gestionar permisos y accesos.</param>
        public UserController(ApplicationDbContext context, IAuthorizationService authorizationServices)
        {
            _context = context;
            _authorizationServices = authorizationServices;
        }

        /// <summary>
        /// Recupera una lista de usuarios activos desde la base de datos.
        /// </summary>
        /// <returns>Una lista de usuarios activos en formato DTO, o un mensaje de error si ocurre un fallo durante la recuperación.</returns>
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _context.Users.Where(u => u.Status.ToLower() != "inactive").ToListAsync();

                var dtoList = users.Select(MapToUserResponseDTO).ToList();

                return Ok(new { success = true, data = dtoList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving users.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Recupera un usuario activo de la base de datos utilizando su dirección de correo electrónico.
        /// </summary>
        /// <param name="email">La dirección de correo electrónico del usuario a buscar.</param>
        /// <returns>El usuario encontrado en formato DTO, o un mensaje de error si el correo electrónico es inválido, el usuario no existe, o ocurre un fallo durante la recuperación.</returns>
        [HttpGet("by-email")]
        public async Task<IActionResult> GetUserByEmail([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { success = false, message = "Email is required." });
            }

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);

                if (user == null || user.Status.ToLower() == "inactive")
                {
                    return NotFound(new { success = false, message = $"User with email {email} not found or is inactive." });
                }

                return Ok(new { success = true, data = MapToUserResponseDTO(user) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving the user.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Recupera un usuario activo de la base de datos utilizando su ID único.
        /// </summary>
        /// <param name="id">El ID del usuario a buscar.</param>
        /// <returns>El usuario encontrado en formato DTO, o un mensaje de error si el usuario no existe, está inactivo, o ocurre un fallo durante la recuperación.</returns>
        [HttpGet("by-id/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(i => i.UserId == id);

                if (user == null || user.Status.ToLower() == "inactive")
                {
                    return NotFound(new { success = false, message = $"User with ID {id} not found or is inactive." });
                }

                return Ok(new { success = true, data = MapToUserResponseDTO(user) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving the user.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Crea un nuevo usuario en la base de datos utilizando los datos proporcionados.
        /// </summary>
        /// <param name="pUser ">Los datos del nuevo usuario a crear, encapsulados en un objeto <see cref="CreateUser DTO"/>.</param>
        /// <returns>Un mensaje de éxito si el usuario se crea correctamente, o un mensaje de error si los datos son inválidos, el usuario ya existe, o ocurre un fallo durante la creación.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDTO pUser)
        {
            if (pUser == null)
            {
                return BadRequest(new { success = false, message = "User data cannot be empty." });
            }

            try
            {
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == pUser.Email);

                if (existingUser != null)
                {
                    if (existingUser.Status.ToLower() == "active")
                    {
                        return Conflict(new { success = false, message = "A user with this email already exists and is active. Please try logging in." });
                    }
                    else
                    {
                        return Conflict(new { success = false, message = "A user with this email already exists but is inactive. Please use the 'Reactivate Account' option to regain access." });
                    }
                }

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(pUser.Password);

                var newUser = new User
                {
                    FullName = pUser.FullName,
                    Email = pUser.Email,
                    Password = hashedPassword,
                    Role = pUser.Role,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "User created successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while creating the user.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Desactiva un usuario en la base de datos cambiando su estado a 'Inactivo'.
        /// </summary>
        /// <param name="id">El ID del usuario a desactivar.</param>
        /// <returns>Un mensaje de éxito si la desactivación es exitosa, o un mensaje de error si el usuario no se encuentra o ya está inactivo, o si ocurre un fallo durante el proceso.</returns>
        [HttpPut("deactivate/{id}")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);

                if (user == null)
                {
                    return NotFound(new { success = false, message = $"User with ID {id} not found." });
                }

                if (user.Status.ToLower() == "inactive")
                {
                    return Ok(new { success = true, message = $"User with ID {id} is already inactive." });
                }

                user.Status = "Inactive";

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = $"User {id} deactivated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while deactivating the user.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Reactiva un usuario en la base de datos utilizando los datos de inicio de sesión proporcionados.
        /// </summary>
        /// <param name="reactivationData">Los datos de reactivación del usuario, encapsulados en un objeto <see cref="LoginUser DTO"/>.</param>
        /// <returns>Un mensaje de éxito si la reactivación es exitosa, o un mensaje de error si los datos son inválidos, el usuario no existe, ya está activo, o si ocurre un fallo durante el proceso.</returns>
        [HttpPost("reactivate")]
        public async Task<IActionResult> ReactivateUser([FromBody] LoginUserDTO reactivationData)
        {
            if (reactivationData == null || string.IsNullOrWhiteSpace(reactivationData.Email) || string.IsNullOrWhiteSpace(reactivationData.Password))
            {
                return BadRequest(new { success = false, message = "Email and password are required for reactivation." });
            }

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.Equals(reactivationData.Email));

                if (user == null)
                {
                    return NotFound(new { success = false, message = "No user found with the provided email address." });
                }

                if (user.Status.ToLower() == "active")
                {
                    return Unauthorized(new { success = false, message = "This account is already active. Please try logging in." });
                }

                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(reactivationData.Password, user.Password);

                if (!isPasswordValid)
                {
                    
                    if (!user.Password.StartsWith("$2a$") && !user.Password.StartsWith("$2b$") && !user.Password.StartsWith("$2y$") || user.Password.Length < 255) // Length check is a weak heuristic
                    {
                        if (user.Password.Equals(reactivationData.Password))
                        {
                            try
                            {
                                user.Password = BCrypt.Net.BCrypt.HashPassword(reactivationData.Password);
                                _context.Users.Update(user);
                                await _context.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error re-hashing password during reactivation for {user.Email}: {ex.Message}");
                            }

                            isPasswordValid = true;
                        }
                    }
                }

                if (!isPasswordValid)
                {
                    return Unauthorized(new { success = false, message = "Invalid credentials. Please check your email and password." });
                }

                
                user.Status = "Active"; 
                                        
                _context.Users.Update(user); 
                await _context.SaveChangesAsync(); 

                return Ok(new { success = true, message = $"Account for {user.Email} reactivated successfully. You can now log in." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred during account reactivation.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Mapea un objeto <see cref="User "/> a un objeto <see cref="User Response DTO"/>.
        /// </summary>
        /// <param name="u">El objeto <see cref="User "/> que se va a mapear.</param>
        /// <returns>Un objeto <see cref="User Response DTO"/> que representa al usuario con los campos relevantes.</returns>
        private static UserResponseDTO MapToUserResponseDTO(User u) => new()
        {
            UserId = u.UserId,
            FullName = u.FullName,
            Email = u.Email,
            Role = u.Role
        };
    }
}
