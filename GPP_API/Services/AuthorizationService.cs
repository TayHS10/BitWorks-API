using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GPP_API.DTO.User;
using GPP_API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace GPP_API.Services
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="AuthorizationService"/>.
        /// </summary>
        /// <param name="configuration">La configuración de la aplicación, utilizada para acceder a los valores de configuración.</param>
        /// <param name="context">El contexto de la base de datos, utilizado para interactuar con la base de datos de la aplicación.</param>
        public AuthorizationService(IConfiguration configuration, ApplicationDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        /// <summary>
        /// Genera y devuelve un token de autorización para un usuario que intenta iniciar sesión.
        /// </summary>
        /// <param name="authorization">Los datos de inicio de sesión del usuario, encapsulados en un objeto <see cref="LoginUser  DTO"/>.</param>
        /// <returns>Un objeto <see cref="AuthorizationResponse"/> que contiene el token generado y el estado del resultado de la autorización, o null si las credenciales son inválidas o el usuario está inactivo.</returns>
        public async Task<AuthorizationResponse> ReturnToken(LoginUserDTO authorization)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.Equals(authorization.Email));

            if (user == null)
            {
                return await Task.FromResult<AuthorizationResponse>(null);
            }

            if (user.Status.ToLower() == "inactive")
            {
                return await Task.FromResult<AuthorizationResponse>(null);
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(authorization.Password, user.Password);

            if (!isPasswordValid)
            {
                return await Task.FromResult<AuthorizationResponse>(null);
            }

            string tokenCreated = GenerateToken(authorization.Email, user.Role);

            return new AuthorizationResponse()
            {
                Token = tokenCreated,
                Result = true,
                Msj = "OK"
            };
        }

        /// <summary>
        /// Genera un token JWT (JSON Web Token) para un usuario basado en su correo electrónico y rol.
        /// </summary>
        /// <param name="pEmail">El correo electrónico del usuario, utilizado como identificador en el token.</param>
        /// <param name="pRole">El rol del usuario, que se incluye como un reclamo en el token.</param>
        /// <returns>El token JWT generado como una cadena.</returns>
        private string GenerateToken(string pEmail, string pRole)
        {
            var key = _configuration.GetValue<string>("JwtSettings:Key");
            var keyBytes = Encoding.ASCII.GetBytes(key);

            var claims = new ClaimsIdentity();
            claims.AddClaim(new Claim(ClaimTypes.NameIdentifier, pEmail));
            claims.AddClaim(new Claim(ClaimTypes.Name, pEmail));

            if (!string.IsNullOrEmpty(pRole))
            {
                claims.AddClaim(new Claim(ClaimTypes.Role, pRole));
            }

            var credentialsToken = new SigningCredentials(
                new SymmetricSecurityKey(keyBytes),
                SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claims,
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = credentialsToken
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenConfig = tokenHandler.CreateToken(tokenDescriptor);
            var tokenCreated = tokenHandler.WriteToken(tokenConfig);

            return tokenCreated;
        }
    }
}