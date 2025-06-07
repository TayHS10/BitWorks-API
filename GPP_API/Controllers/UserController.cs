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
        // Declares a private, read-only field for database interaction.
        // 'ApplicationDbContext' is the Entity Framework Core database context class.
        // '_context' is the conventional name for the context instance.
        private readonly ApplicationDbContext _context;

        // Declares a private, read-only field for the authorization service.
        // 'IAuthorizationService' is an ASP.NET Core interface used for evaluating authorization policies.
        // '_authorizationServices' is the conventional name for this service instance.
        private readonly IAuthorizationService _authorizationServices;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserController"/> class.
        /// </summary>
        /// <param name="context">The database context for interacting with application data.</param>
        /// <param name="authorizationServices">The authorization service for evaluating permission policies.</param>
        public UserController(ApplicationDbContext context, IAuthorizationService authorizationServices)
        {
            _context = context;
            _authorizationServices = authorizationServices;
        }

        /// <summary>
        /// Retrieves a list of all active users from the system.
        /// </summary>
        /// <returns>
        /// An <see cref="IActionResult"/> representing the HTTP response. Returns HTTP 200 OK with user data on success,
        /// or HTTP 500 Internal Server Error if an exception occurs.
        /// </returns>
        /// <response code="200">Returns a list of active users.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                // Query the database to retrieve all users.
                // Filter out users whose 'Status' is "inactive" (case-insensitive comparison).
                var users = await _context.Users.Where(u => u.Status.ToLower() != "inactive").ToListAsync();

                // Map the list of 'User' entities (domain models) to a list of 'UserResponseDTO' objects.
                // This transformation ensures that only necessary and safe data is exposed to the client.
                var dtoList = users.Select(MapToUserResponseDTO).ToList();

                // Return an HTTP 200 OK response, indicating success and providing the list of mapped user data.
                return Ok(new { success = true, data = dtoList });
            }
            catch (Exception ex)
            {
                // Catch any unexpected exceptions that occur during the process.
                // Return an HTTP 500 Internal Server Error response.
                // Include a generic error message and the exception's detailed message for debugging.
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving users.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves an active user by their email address.
        /// </summary>
        /// <param name="email">The email address of the user to retrieve.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> representing the HTTP response.
        /// Returns HTTP 200 OK if found, 400 Bad Request if email is missing, 404 Not Found if not found or inactive,
        /// or 500 Internal Server Error for other exceptions.
        /// </returns>
        /// <response code="200">Returns the active user's data.</response>
        /// <response code="400">If the email parameter is missing or empty.</response>
        /// <response code="404">If no active user is found with the provided email.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet("by-email")]
        public async Task<IActionResult> GetUserByEmail([FromQuery] string email)
        {
            // Validate if the email query parameter is provided and not empty or whitespace.
            if (string.IsNullOrWhiteSpace(email))
            {
                // Return an HTTP 400 Bad Request response if the email is invalid.
                return BadRequest(new { success = false, message = "Email is required." });
            }

            try
            {
                // Attempt to retrieve a user from the database by their email address.
                // It specifically looks for an exact match on the email.
                var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);

                // Check if the user was found and if their status is not "inactive".
                // The status comparison is case-insensitive.
                if (user == null || user.Status.ToLower() == "inactive")
                {
                    // Return an HTTP 404 Not Found response if the user doesn't exist or is inactive.
                    return NotFound(new { success = false, message = $"User with email {email} not found or is inactive." });
                }

                // If an active user is found, map the user entity to a UserResponseDTO.
                // This ensures a standardized and secure data representation for the client.
                return Ok(new { success = true, data = MapToUserResponseDTO(user) });
            }
            catch (Exception ex)
            {
                // Catch any unforeseen exceptions that occur during database access or processing.
                // It's highly recommended to log 'ex' here for monitoring and debugging in production environments.
                // Return an HTTP 500 Internal Server Error, providing a general error message and the exception details.
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving the user.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves an active user by their unique ID.
        /// </summary>
        /// <param name="id">The unique identifier of the user to retrieve.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> representing the HTTP response.
        /// Returns HTTP 200 OK if found, 404 Not Found if not found or inactive,
        /// or 500 Internal Server Error for other exceptions.
        /// </returns>
        /// <response code="200">Returns the active user's data.</response>
        /// <response code="404">If no active user is found with the provided ID.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet("by-id/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                // Attempt to retrieve a user from the database by their unique UserId.
                var user = await _context.Users.FirstOrDefaultAsync(i => i.UserId == id);

                // Check if the user was found and if their status is not "inactive".
                // The status comparison is case-insensitive.
                if (user == null || user.Status.ToLower() == "inactive")
                {
                    // Return an HTTP 404 Not Found response if the user doesn't exist or is inactive.
                    return NotFound(new { success = false, message = $"User with ID {id} not found or is inactive." });
                }

                // If an active user is found, map the user entity to a UserResponseDTO.
                // This ensures a standardized and secure data representation for the client.
                return Ok(new { success = true, data = MapToUserResponseDTO(user) });
            }
            catch (Exception ex)
            {
                // Catch any unforeseen exceptions that occur during database access or processing.
                // It's highly recommended to log the exception 'ex' here for debugging and monitoring purposes
                // in production environments.
                // Return an HTTP 500 Internal Server Error, providing a general error message and the exception details.
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving the user.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Creates a new user in the system.
        /// </summary>
        /// <param name="pUser">The data required to create a new user (FullName, Email, Password).</param>
        /// <returns>
        /// An <see cref="IActionResult"/> representing the HTTP response.
        /// Returns HTTP 200 OK on successful creation, 400 Bad Request if input is invalid,
        /// 409 Conflict if a user with the email already exists (active or inactive),
        /// or 500 Internal Server Error for other exceptions.
        /// </returns>
        /// <response code="200">Returns success message upon user creation.</response>
        /// <response code="400">If the provided user data is null or empty.</response>
        /// <response code="409">If a user with the specified email already exists (active or inactive).</response>
        /// <response code="500">If an internal server error occurs during user creation.</response>
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDTO pUser)
        {
            // Validate if the incoming user data is null.
            if (pUser == null)
            {
                // Return an HTTP 400 Bad Request if no user data is provided.
                return BadRequest(new { success = false, message = "User data cannot be empty." });
            }

            try
            {
                // Check if a user with the provided email already exists in the database.
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == pUser.Email);

                // If a user with the email is found, handle conflict scenarios.
                if (existingUser != null)
                {
                    // If the existing user is active, return a 409 Conflict indicating the user already exists.
                    if (existingUser.Status.ToLower() == "active")
                    {
                        return Conflict(new { success = false, message = "A user with this email already exists and is active. Please try logging in." });
                    }
                    else // existingUser.Status.ToLower() == "inactive"
                    {
                        // If the existing user is inactive, return a 409 Conflict suggesting reactivation.
                        return Conflict(new { success = false, message = "A user with this email already exists but is inactive. Please use the 'Reactivate Account' option to regain access." });
                    }
                }

                // If no user exists with this email (neither active nor inactive), proceed with new user creation.
                // Hash the provided password for secure storage.
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(pUser.Password);

                // Create a new User entity with the provided data and default values.
                var newUser = new User
                {
                    FullName = pUser.FullName,
                    Email = pUser.Email,
                    Password = hashedPassword, // Store the hashed password, not the plain text.
                    Role = pUser.Role,       // Assign the role.
                    Status = "Active",         // Set the default status for new users.
                    CreatedAt = DateTime.UtcNow // Set the creation timestamp to the current UTC time.
                };

                // Add the new user entity to the DbContext and save changes to the database.
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // Return an HTTP 200 OK response indicating successful user creation.
                return Ok(new { success = true, message = "User created successfully." });
            }
            catch (Exception ex)
            {
                // Catch any unexpected exceptions that occur during the user creation process.
                // It is strongly recommended to log the exception 'ex' here for debugging and monitoring
                // in production environments, using a dedicated logging framework (e.g., Serilog, NLog).
                // Return an HTTP 500 Internal Server Error, providing a general error message and the exception details.
                return StatusCode(500, new { success = false, message = "An error occurred while creating the user.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Deactivates a user by setting their status to 'Inactive' (soft delete).
        /// </summary>
        /// <param name="id">The unique identifier of the user to deactivate.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> representing the HTTP response.
        /// Returns HTTP 200 OK on successful deactivation or if already inactive.
        /// Returns HTTP 404 Not Found if the user does not exist.
        /// Returns HTTP 500 Internal Server Error for any unexpected exceptions.
        /// </returns>
        /// <response code="200">Returns success message upon user deactivation or if already inactive.</response>
        /// <response code="404">If no user is found with the provided ID.</response>
        /// <response code="500">If an internal server error occurs during deactivation.</response>
        [HttpPut("deactivate/{id}")] // HttpPut is suitable for updating resource state, including status changes.
        public async Task<IActionResult> DeactivateUser(int id)
        {
            try
            {
                // Find the user in the database by their unique ID.
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);

                // If no user is found with the given ID, return a 404 Not Found response.
                if (user == null)
                {
                    return NotFound(new { success = false, message = $"User with ID {id} not found." });
                }

                // Check if the user's status is already 'Inactive' (case-insensitive) to prevent redundant updates.
                if (user.Status.ToLower() == "inactive")
                {
                    // Return HTTP 200 OK, indicating that the desired state is already met.
                    return Ok(new { success = true, message = $"User with ID {id} is already inactive." });
                }

                // Change the user's status to 'Inactive' to perform a soft delete.
                user.Status = "Inactive";

                // Mark the user entity as modified in the DbContext and save changes to the database.
                _context.Users.Update(user); // Entity Framework Core tracks changes, explicitly Update() is often optional but harmless.
                await _context.SaveChangesAsync();

                // Return an HTTP 200 OK response, confirming successful deactivation.
                return Ok(new { success = true, message = $"User {id} deactivated successfully." });
            }
            catch (Exception ex)
            {
                // Catch any unforeseen exceptions during the deactivation process.
                // It is crucial to log the exception 'ex' here for debugging and monitoring in production environments.
                // Return an HTTP 500 Internal Server Error, providing a general error message and the exception details.
                return StatusCode(500, new { success = false, message = "An error occurred while deactivating the user.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Reactivates an existing inactive user account using provided login credentials.
        /// </summary>
        /// <param name="reactivationData">The login credentials (email and password) for the inactive account to reactivate.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> representing the HTTP response.
        /// Returns HTTP 200 OK on successful reactivation.
        /// Returns HTTP 400 Bad Request if input data is invalid.
        /// Returns HTTP 404 Not Found if no user with the email exists.
        /// Returns HTTP 401 Unauthorized if credentials are invalid or the account is already active.
        /// Returns HTTP 500 Internal Server Error for other exceptions.
        /// </returns>
        /// <response code="200">Returns success message upon account reactivation.</response>
        /// <response code="400">If email or password is missing from the request body.</response>
        /// <response code="404">If no user is found with the provided email address.</response>
        /// <response code="401">If the account is already active or if credentials are invalid.</response>
        /// <response code="500">If an internal server error occurs during reactivation.</response>
        [HttpPost("reactivate")] // HttpPost is used as sensitive data (credentials) are sent in the request body to perform an action.
        public async Task<IActionResult> ReactivateUser([FromBody] LoginUserDTO reactivationData)
        {
            // Validate the incoming reactivation data: ensure it's not null and both email and password are provided.
            if (reactivationData == null || string.IsNullOrWhiteSpace(reactivationData.Email) || string.IsNullOrWhiteSpace(reactivationData.Password))
            {
                // Return HTTP 400 Bad Request if essential data is missing.
                return BadRequest(new { success = false, message = "Email and password are required for reactivation." });
            }

            try
            {
                // 1. Find the user by their email address in the database.
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.Equals(reactivationData.Email));

                // If no user is found with the provided email, return HTTP 404 Not Found.
                if (user == null)
                {
                    return NotFound(new { success = false, message = "No user found with the provided email address." });
                }

                // 2. Check if the found account is already active.
                // If it is, return HTTP 401 Unauthorized, suggesting a regular login instead.
                if (user.Status.ToLower() == "active")
                {
                    return Unauthorized(new { success = false, message = "This account is already active. Please try logging in." });
                }

                // 3. Verify the provided plain-text password against the stored hashed password.
                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(reactivationData.Password, user.Password);

                // This block handles potential password migration from plain-text to BCrypt hash.
                if (!isPasswordValid)
                {
                    // Heuristic check: If BCrypt verification failed, and the stored password doesn't look like a BCrypt hash,
                    // attempt to verify against a legacy plain-text password.
                    // BCrypt hashes typically start with "$2a$", "$2b$", or "$2y$" and are long (e.g., 60 chars).
                    if (!user.Password.StartsWith("$2a$") && !user.Password.StartsWith("$2b$") && !user.Password.StartsWith("$2y$") || user.Password.Length < 255) // Length check is a weak heuristic
                    {
                        // === FALLBACK TO OLD PLAIN-TEXT PASSWORD COMPARISON ===
                        if (user.Password.Equals(reactivationData.Password))
                        {
                            // If the plain-text password matches, re-hash and update the user's password for security.
                            try
                            {
                                user.Password = BCrypt.Net.BCrypt.HashPassword(reactivationData.Password);
                                _context.Users.Update(user); // Mark user entity as modified.
                                await _context.SaveChangesAsync(); // Persist the updated hash.
                            }
                            catch (Exception ex)
                            {
                                // Log errors during re-hashing but do not prevent reactivation if the old password was valid.
                                Console.WriteLine($"Error re-hashing password during reactivation for {user.Email}: {ex.Message}");
                            }

                            isPasswordValid = true; // Mark as valid to proceed with reactivation.
                        }
                    }
                }

                // If password verification (either BCrypt or legacy plain-text) fails, return Unauthorized.
                if (!isPasswordValid)
                {
                    return Unauthorized(new { success = false, message = "Invalid credentials. Please check your email and password." });
                }

                // 4. If credentials are valid and the account is inactive, proceed to reactivate the account.
                user.Status = "Active"; // Set the user's status to 'Active'.
                                        // Optionally, update other fields like LastLoginDate, etc.
                _context.Users.Update(user); // Mark user entity as modified.
                await _context.SaveChangesAsync(); // Persist the status change.

                // Return HTTP 200 OK, confirming successful account reactivation.
                return Ok(new { success = true, message = $"Account for {user.Email} reactivated successfully. You can now log in." });
            }
            catch (Exception ex)
            {
                // Catch any unexpected exceptions during the reactivation process.
                // It is critical to log the exception 'ex' here for debugging and monitoring in production environments.
                // Return HTTP 500 Internal Server Error, providing a general error message and the exception details.
                return StatusCode(500, new { success = false, message = "An error occurred during account reactivation.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Maps a <see cref="User"/> entity to a <see cref="UserResponseDTO"/> object.
        /// </summary>
        /// <param name="u">The <see cref="User"/> entity to map.</param>
        /// <returns>A new <see cref="UserResponseDTO"/> object populated with the mapped values from the user entity.</returns>
        private static UserResponseDTO MapToUserResponseDTO(User u) => new()
        {
            UserId = u.UserId,
            FullName = u.FullName,
            Email = u.Email,
            Role = u.Role
        };

    }
}
