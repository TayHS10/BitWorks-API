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
        private readonly GestionPresupuestariaDbContext _context;
        private readonly IAuthorizationService _authorizationServices;

        public UserController(GestionPresupuestariaDbContext context,
            IAuthorizationService authorizationServices)
        {
            _context = context;
            _authorizationServices = authorizationServices;
        }

        /// <summary>
        /// Gets a list of all active users.
        /// </summary>
        /// <returns>
        /// An HTTP 200 OK response with a list of active user data if successful.
        /// An HTTP 500 Internal Server Error if an unexpected error occurs during retrieval.
        /// </returns>
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                // Retrieve all users and filter out inactive ones.
                var users = await _context.Users.Where(u => u.Status.ToLower() != "inactive").ToListAsync();

                // Maps the list of User entities to a list of UserDTO objects.
                var dtoList = users.Select(MapToUserDTO).ToList();

                return Ok(new { success = true, data = dtoList });
            }
            catch (Exception ex)
            {
                // It's highly recommended to log the exception (ex) here for debugging and monitoring purposes.
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving users.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves an active user by their email address.
        /// </summary>
        /// <param name="email">The email address of the user to retrieve.</param>
        /// <returns>
        /// An HTTP 200 OK response with the user data if found and active.
        /// An HTTP 400 Bad Request if the email address is not provided or is empty.
        /// An HTTP 404 Not Found response if no active user with the specified email address exists.
        /// An HTTP 500 Internal Server Error if an unexpected error occurs during the retrieval process.
        /// </returns>
        [HttpGet("by-email")]
        public async Task<IActionResult> GetUserByEmail([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { success = false, message = "Email is required." });
            }

            try
            {
                // Retrieve the user by email and ensure they are active.
                var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);

                // Check if the user exists and is active.
                if (user == null || user.Status.ToLower() == "inactive")
                {
                    return NotFound(new { success = false, message = $"User with email {email} not found or is inactive." });
                }

                return Ok(new { success = true, data = MapToUserDTO(user) });
            }
            catch (Exception ex)
            {
                // Consider logging the exception 'ex' for debugging and monitoring purposes.
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving the user.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves an active user by their unique ID.
        /// </summary>
        /// <param name="id">The unique identifier of the user to retrieve.</param>
        /// <returns>
        /// An HTTP 200 OK response with the user data if found and active.
        /// An HTTP 404 Not Found response if no active user with the specified ID exists.
        /// An HTTP 500 Internal Server Error if an unexpected error occurs during the retrieval process.
        /// </returns>
        [HttpGet("by-id/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                // Retrieve the user by ID.
                var user = await _context.Users.FirstOrDefaultAsync(i => i.UserId == id);

                // Check if the user exists and is active.
                if (user == null || user.Status.ToLower() == "inactive")
                {
                    return NotFound(new { success = false, message = $"User with ID {id} not found or is inactive." });
                }

                return Ok(new { success = true, data = MapToUserDTO(user) });
            }
            catch (Exception ex)
            {
                // It's good practice to log the exception (ex) here for debugging purposes.
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving the user.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Creates a new user in the system.
        /// If an inactive user with the same email exists, it will prevent new creation and suggest reactivation.
        /// The user's password will be hashed before being stored for security reasons.
        /// </summary>
        /// <param name="pUser">The user data to create. It should contain FullName, Email, and Password.</param>
        /// <returns>
        /// An HTTP 200 OK response if the user is created successfully.
        /// An HTTP 400 Bad Request if the provided user data is empty.
        /// An HTTP 409 Conflict if an active user with the same email already exists, or if an inactive user exists (suggesting reactivation).
        /// An HTTP 500 Internal Server Error if an unexpected error occurs during user creation.
        /// </returns>
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] UserDTO pUser)
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
                        // Conflict if an active user already exists with this email
                        return Conflict(new { success = false, message = "A user with this email already exists and is active. Please try logging in." });
                    }
                    else // existingUser.Status.ToLower() == "inactive"
                    {
                        // Conflict if an inactive user already exists, suggesting reactivation
                        return Conflict(new { success = false, message = "A user with this email already exists but is inactive. Please use the 'Reactivate Account' option to regain access." });
                    }
                }

                // If no user exists with this email (neither active nor inactive), proceed with new creation
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(pUser.Password);

                var newUser = new User
                {
                    FullName = pUser.FullName,
                    Email = pUser.Email,
                    Password = hashedPassword, // Store the hashed password
                    Role = "Accountant",       // Default role for new users
                    Status = "Active",         // Default status for new users
                    CreatedAt = DateTime.UtcNow // Sets the creation timestamp to the current UTC time
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "User created successfully." });
            }
            catch (Exception ex)
            {
                // Logs the exception for debugging purposes.
                // It's recommended to use a logging framework for production environments.
                return StatusCode(500, new { success = false, message = "An error occurred while creating the user.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Deactivates a user by setting their status to 'Inactive' instead of permanently deleting them.
        /// This is a soft delete operation.
        /// </summary>
        /// <param name="id">The unique identifier of the user to deactivate.</param>
        /// <returns>
        /// An HTTP 200 OK response if the user is successfully deactivated.
        /// An HTTP 404 Not Found response if no user with the specified ID exists.
        /// An HTTP 500 Internal Server Error if an unexpected error occurs during the deactivation process.
        /// </returns>
        [HttpPut("deactivate/{id}")] // Using HttpPut is common for updates, including status changes
        public async Task<IActionResult> DeactivateUser(int id)
        {
            try
            {
                // Find the user by their ID.
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);

                if (user == null)
                {
                    return NotFound(new { success = false, message = $"User with ID {id} not found." });
                }

                // Check if the user is already inactive to avoid unnecessary updates.
                if (user.Status.ToLower() == "inactive")
                {
                    return Ok(new { success = true, message = $"User with ID {id} is already inactive." });
                }

                // Set the user's status to 'Inactive'.
                user.Status = "Inactive";

                // Update the user entity in the database.
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = $"User {id} deactivated successfully." });
            }
            catch (Exception ex)
            {
                // Log the exception for debugging and monitoring purposes.
                return StatusCode(500, new { success = false, message = "An error occurred while deactivating the user.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Reactivates an existing inactive user account using provided login credentials.
        /// </summary>
        /// <param name="reactivationData">The login credentials (email and password) for the inactive account to reactivate.</param>
        /// <returns>
        /// An HTTP 200 OK response with a success message if the account is successfully reactivated.
        /// An HTTP 400 Bad Request if the provided data is invalid.
        /// An HTTP 404 Not Found if no user with the specified email exists.
        /// An HTTP 401 Unauthorized if the provided credentials are invalid or the account is already active.
        /// An HTTP 500 Internal Server Error if an unexpected error occurs during reactivation.
        /// </returns>
        [HttpPost("reactivate")] // Using HttpPost as we are sending data (LoginDTO) to perform an action
        public async Task<IActionResult> ReactivateUser([FromBody] LoginDTO reactivationData)
        {
            if (reactivationData == null || string.IsNullOrWhiteSpace(reactivationData.Email) || string.IsNullOrWhiteSpace(reactivationData.Password))
            {
                return BadRequest(new { success = false, message = "Email and password are required for reactivation." });
            }

            try
            {
                // 1. Find the user by email
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.Equals(reactivationData.Email));

                if (user == null)
                {
                    // User not found (or email incorrect)
                    return NotFound(new { success = false, message = "No user found with the provided email address." });
                }

                // 2. Check if the account is already active
                if (user.Status.ToLower() == "active")
                {
                    return Unauthorized(new { success = false, message = "This account is already active. Please try logging in." });
                }

                // 3. Verify the provided password against the stored hashed password
                // This also handles the re-hashing logic if the password was previously plain text
                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(reactivationData.Password, user.Password);

                if (!isPasswordValid)
                {
                    // If BCrypt verification fails, check if the stored password is NOT already a BCrypt hash.
                    // This heuristic helps identify old, plain-text passwords for migration during reactivation.
                    if (!user.Password.StartsWith("$2a$") && !user.Password.StartsWith("$2b$") && !user.Password.StartsWith("$2y$") || user.Password.Length < 255)
                    {
                        // === FALLBACK TO OLD PLAIN-TEXT PASSWORD COMPARISON ===
                        if (user.Password.Equals(reactivationData.Password))
                        {
                            // Password matches the old plain-text password!
                            // === IMPORTANT: RE-HASH AND UPDATE THE PASSWORD DURING REACTIVATION ===
                            try
                            {
                                user.Password = BCrypt.Net.BCrypt.HashPassword(reactivationData.Password);
                                _context.Users.Update(user);
                                await _context.SaveChangesAsync();
                                // Log: Password for user {user.Email} successfully re-hashed during reactivation.
                            }
                            catch (Exception ex)
                            {
                                // Log the re-hashing error, but don't prevent reactivation if old password matched.
                                Console.WriteLine($"Error re-hashing password during reactivation for {user.Email}: {ex.Message}");
                            }

                            isPasswordValid = true; // Set to true to proceed with reactivation
                        }
                    }
                }

                if (!isPasswordValid)
                {
                    // If neither new nor old method worked (or old method failed), authentication fails.
                    return Unauthorized(new { success = false, message = "Invalid credentials. Please check your email and password." });
                }

                // 4. If credentials are valid and account is inactive, reactivate the account
                user.Status = "Active";
                // Optionally, update other fields like LastLogin, etc.
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = $"Account for {user.Email} reactivated successfully. You can now log in." });
            }
            catch (Exception ex)
            {
                // Log the exception for debugging and monitoring purposes.
                return StatusCode(500, new { success = false, message = "An error occurred during account reactivation.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Authenticates a user and returns an authorization token.
        /// </summary>
        /// <param name="authenticated">The login credentials (e.g., username and password).</param>
        /// <returns>
        /// An HTTP 200 OK response with the authorization token if authentication is successful.
        /// An HTTP 401 Unauthorized response if authentication fails.
        /// </returns>
        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] LoginDTO authenticated)
        {
            var authorized = await _authorizationServices.ReturnToken(authenticated);

            if (authorized == null)
            {
                return Unauthorized(new { success = false, message = "Invalid credentials." });
            }

            return Ok(authorized);
        }

        /// <summary>
        /// Maps a User object to a UserDTO object.
        /// </summary>
        /// <param name="u">The User object to map.</param>
        /// <returns>A new UserDTO object with mapped values.</returns>
        private static UserDTO MapToUserDTO(User u) => new()
        {
            UserId = u.UserId,
            FullName = u.FullName,
            Email = u.Email,
            //Password = u.Password, // Password is intentionally excluded for security reasons!!
            Role = u.Role
        };
    }
}
