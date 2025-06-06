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
        /// Initializes a new instance of the <see cref="AuthorizationService"/> class.
        /// </summary>
        /// <param name="configuration">The application's configuration provider, used for accessing settings like JWT secret keys.</param>
        /// <param name="context">The database context for interacting with application data, specifically user data for authentication.</param>
        public AuthorizationService(IConfiguration configuration, ApplicationDbContext context)
        {
            // Assigns the injected IConfiguration instance to the _configuration field.
            // This allows the service to access application settings (e.g., JWT secret).
            _configuration = configuration;

            // Assigns the injected ApplicationDbContext instance to the _context field.
            // This allows the service to query user data for authentication and authorization.
            _context = context;
        }

        /// <summary>
        /// Authenticates a user based on provided login credentials and returns an authorization token.
        /// </summary>
        /// <param name="authorization">The login data, typically containing the user's email and plain text password.</param>
        /// <returns>
        /// An <see cref="AuthorizationResponse"/> object containing the generated token if authentication is successful and the account is active,
        /// or <see langword="null"/> if authentication fails (e.g., user not found, invalid credentials, or inactive account).
        /// </returns>
        public async Task<AuthorizationResponse> ReturnToken(LoginUserDTO authorization)
        {
            // Attempt to find a user in the database matching the provided email.
            // The password is not used in this query as it's hashed and not directly searchable.
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.Equals(authorization.Email));

            // If no matching user is found by email, authentication fails.
            if (user == null)
            {
                return await Task.FromResult<AuthorizationResponse>(null);
            }

            // Check if the user account is currently inactive.
            // If the account is inactive, prevent login and return null to indicate authentication failure.
            if (user.Status.ToLower() == "inactive")
            {
                // Depending on application requirements, a specific error message might be logged or returned here.
                return await Task.FromResult<AuthorizationResponse>(null);
            }

            // Verify the provided plain-text password against the hashed password stored in the database.
            // BCrypt.Net.BCrypt.Verify handles the salting and hashing comparison internally.
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(authorization.Password, user.Password);

            // If the provided password does not match the stored hash, authentication fails.
            if (!isPasswordValid)
            {
                return await Task.FromResult<AuthorizationResponse>(null);
            }

            // If authentication is successful and the account is active, generate a new authorization token.
            string tokenCreated = GenerateToken(authorization.Email);

            // Return a successful authorization response containing the newly generated token and status.
            return new AuthorizationResponse()
            {
                Token = tokenCreated,
                Result = true,
                Msj = "OK" // A success message for the client.
            };
        }

        /// <summary>
        /// Generates a JSON Web Token (JWT) for a given email address.
        /// </summary>
        /// <param name="pEmail">The email address to include in the token's claims as the name identifier.</param>
        /// <returns>A string representing the generated JWT.</returns>
        private string GenerateToken(string pEmail)
        {
            // Retrieve the JWT secret key from the application's configuration (e.g., appsettings.json).
            var key = _configuration.GetValue<string>("JwtSettings:Key");

            // Convert the secret key string into a byte array, as required for cryptographic operations.
            var keyBytes = Encoding.ASCII.GetBytes(key);

            // Create a new ClaimsIdentity object to encapsulate the claims that will be part of the token.
            var claims = new ClaimsIdentity();
            // Add the user's email as a claim with the type 'NameIdentifier', which uniquely identifies the subject of the token.
            claims.AddClaim(new Claim(ClaimTypes.NameIdentifier, pEmail));

            // Set up the signing credentials using the secret key and the HMAC SHA256 algorithm.
            // This is crucial for verifying the token's integrity and authenticity.
            var credentialsToken = new SigningCredentials(
                new SymmetricSecurityKey(keyBytes),
                SecurityAlgorithms.HmacSha256Signature);

            // Configure the token descriptor, which defines the properties of the JWT.
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claims, // Assign the claims (user identity).
                Expires = DateTime.UtcNow.AddHours(1), // Set token expiration to 1 hour from now (recommended for security).
                SigningCredentials = credentialsToken // Assign the signing credentials.
            };

            // Create an instance of JwtSecurityTokenHandler, which is responsible for creating and validating JWTs.
            var tokenHandler = new JwtSecurityTokenHandler();

            // Create the JWT based on the configured token descriptor.
            var tokenConfig = tokenHandler.CreateToken(tokenDescriptor);

            // Serialize the JWT object into its string representation (the actual token string).
            var tokenCreated = tokenHandler.WriteToken(tokenConfig);

            // Return the generated JWT string.
            return tokenCreated;
        }
    }
}
