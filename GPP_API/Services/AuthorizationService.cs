using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GPP_API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace GPP_API.Services
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly IConfiguration _configuration;

        private readonly GestionPresupuestariaDbContext _context;

        public AuthorizationService(IConfiguration configuration,
           GestionPresupuestariaDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public async Task<AuthorizationResponse> ReturnToken(LoginDTO authorization)
        {
            var temp = await _context.Users.FirstOrDefaultAsync(u =>
            u.Email.Equals(authorization.Email) &&
            u.Password.Equals(authorization.Password));

            if (temp == null)
            {
                return await Task.FromResult<AuthorizationResponse>(null);
            }

            string tokenCreated = GenerateToken(authorization.Email);

            return new AuthorizationResponse()
            {
                Token = tokenCreated,
                Result = true,
                Msj = "OK"
            };

        }

        private string GenerateToken(string pEmail)
        {
            var key = _configuration.GetValue<string>("JwtSettings:Key");

            var keyBytes = Encoding.ASCII.GetBytes(key);

            var claims = new ClaimsIdentity();
            claims.AddClaim(new Claim(ClaimTypes.NameIdentifier, pEmail));

            var credentialsToken = new SigningCredentials(
                new SymmetricSecurityKey(keyBytes),
                SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claims,
                SigningCredentials = credentialsToken
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var tokenConfig = tokenHandler.CreateToken(tokenDescriptor);

            var tokenCreated = tokenHandler.WriteToken(tokenConfig);

            return tokenCreated;
        }
    }
}
