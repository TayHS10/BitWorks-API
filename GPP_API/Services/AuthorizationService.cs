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

        private readonly GestionPresupuestariaDbContext _context;

        public AuthorizationService(IConfiguration configuration,
           GestionPresupuestariaDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        /// <summary>
        /// Authenticates a user based on provided login credentials and returns an authorization token.
        /// It verifies the plain text password against the stored hashed password and ensures the user account is active.
        /// </summary>
        /// <param name="authorization">The login data, typically containing the user's email and plain text password.</param>
        /// <returns>
        /// An <see cref="AuthorizationResponse"/> object containing the generated token if authentication is successful and the account is active,
        /// or a null value if authentication fails (e.g., user not found, invalid credentials, or inactive account).
        /// </returns>
        public async Task<AuthorizationResponse> ReturnToken(LoginDTO authorization)
        {
            // Attempts to find a user in the database matching the provided email.
            // We only search by email here, as the password is hashed and cannot be directly queried.
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.Equals(authorization.Email));

            if (user == null)
            {
                // If no matching user is found by email, authentication fails.
                return await Task.FromResult<AuthorizationResponse>(null);
            }

            // --- NEW: Check if the user account is inactive ---
            if (user.Status.ToLower() == "inactive")
            {
                // If the account is inactive, prevent login.
                // You might return a specific message or error code here depending on your application's needs.
                // For simplicity, we'll return null, indicating authentication failure.
                return await Task.FromResult<AuthorizationResponse>(null);
            }
            // --- END NEW ---

            // Verify the provided plain text password against the hashed password stored in the database.
            // BCrypt.Net.BCrypt.Verify handles the salting internally.
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(authorization.Password, user.Password);

            if (!isPasswordValid)
            {
                // If the password does not match the stored hash, authentication fails.
                return await Task.FromResult<AuthorizationResponse>(null);
            }

            // Generates a new authorization token for the authenticated and active user.
            string tokenCreated = GenerateToken(authorization.Email);

            // Returns a successful authorization response with the generated token.
            return new AuthorizationResponse()
            {
                Token = tokenCreated,
                Result = true,
                Msj = "OK" // Message indicating success.
            };
        }

        /// <summary>
        /// Generates a JSON Web Token (JWT) for a given email address.
        /// </summary>
        /// <param name="pEmail">The email address to include in the token's claims.</param>
        /// <returns>A string representing the generated JWT.</returns>
        private string GenerateToken(string pEmail)
        {
            // Retrieves the JWT secret key from the application's configuration.
            var key = _configuration.GetValue<string>("JwtSettings:Key");

            // Converts the secret key string into a byte array.
            var keyBytes = Encoding.ASCII.GetBytes(key);

            // Creates a new ClaimsIdentity to hold the token's claims.
            var claims = new ClaimsIdentity();
            // Adds a claim for the user's email, using ClaimTypes.NameIdentifier.
            claims.AddClaim(new Claim(ClaimTypes.NameIdentifier, pEmail));

            // Sets up the signing credentials using the secret key and HMAC SHA256 algorithm.
            var credentialsToken = new SigningCredentials(
                new SymmetricSecurityKey(keyBytes),
                SecurityAlgorithms.HmacSha256Signature);

            // Configures the token descriptor with the subject (claims) and signing credentials.
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claims,
                SigningCredentials = credentialsToken
            };

            // Creates a token handler to create and write the JWT.
            var tokenHandler = new JwtSecurityTokenHandler();

            // Creates the JWT based on the token descriptor.
            var tokenConfig = tokenHandler.CreateToken(tokenDescriptor);

            // Writes the JWT to a string format.
            var tokenCreated = tokenHandler.WriteToken(tokenConfig);

            // Returns the generated JWT string.
            return tokenCreated;
        }
    }
}
