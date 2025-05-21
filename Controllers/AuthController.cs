using BackendW2Proj.Data;
using BackendW2Proj.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Cryptography;

namespace BackendW2Proj.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly string _jwtSecret;

        public AuthController(AppDbContext context, IConfiguration configuration) // checks if user JWT secret is valid 
        {
            _context = context;
            _jwtSecret = configuration["JwtSettings:Secret"];
            if (string.IsNullOrEmpty(_jwtSecret) || Encoding.ASCII.GetBytes(_jwtSecret).Length < 32)
            {
                throw new InvalidOperationException("JWT secret key is missing or too short. Ensure it is at least 32 characters long.");
            }
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] UserModel user)
        {
            if (_context.Users.Any(u => u.username == user.username))
            {
                return BadRequest(new { Message = "Username already exists." });
            }

            user.passwordHash = HashPassword(user.passwordHash); // Hash the password
            user.role = string.IsNullOrEmpty(user.role) ? "User" : user.role; // Default to "User" if no role is provided
            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok(new { Message = "User registered successfully." });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] UserModel user)
        {
            var existingUser = _context.Users.FirstOrDefault(u => u.username == user.username);
            if (existingUser == null || !VerifyPassword(user.passwordHash, existingUser.passwordHash))
            {
                return Unauthorized(new { Message = "Invalid username or password." });
            }

            var token = GenerateJwtToken(existingUser.id);
            return Ok(new
            {
                Token = token,
                Role = existingUser.role,
                Name = existingUser.username
            });
        }

        [HttpPut("username")]
        [Authorize]
        public IActionResult ChangeUsername([FromBody] ChangeUsernameRequest request)
        {
            var userId = int.Parse(User.Claims.First(c => c.Type == "id").Value);
            var user = _context.Users.FirstOrDefault(u => u.id == userId);

            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            if (!string.Equals(user.username, request.CurrentUsername, StringComparison.Ordinal))
            {
                return BadRequest(new { Message = "Current username is incorrect." });
            }

            if (_context.Users.Any(u => u.username == request.NewUsername))
            {
                return BadRequest(new { Message = "Username already exists." });
            }

            user.username = request.NewUsername;
            _context.SaveChanges();

            return Ok(new { Message = "Username updated successfully." });
        }





        // Allow authenticated user to change their own password
        public class ChangePasswordRequest
        {
            public string CurrentPassword { get; set; } = string.Empty;
            public string NewPassword { get; set; } = string.Empty;
        }

        public class ChangeUsernameRequest
        {
            public string CurrentUsername { get; set; } = string.Empty;
            public string NewUsername { get; set; } = string.Empty;
        }



        [HttpPut("password")]
        [Authorize]
        public IActionResult ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = int.Parse(User.Claims.First(c => c.Type == "id").Value);
            var user = _context.Users.FirstOrDefault(u => u.id == userId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            if (!VerifyPassword(request.CurrentPassword, user.passwordHash))
            {
                return BadRequest(new { Message = "Current password is incorrect." });
            }

            user.passwordHash = HashPassword(request.NewPassword);
            _context.SaveChanges();

            return Ok(new { Message = "Password updated successfully." });
        }

        [HttpPut("admin/users/{id}/username")]
        [Authorize(Roles = "Admin")]
        public IActionResult AdminChangeUsername(int id, [FromBody] ChangeUsernameRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NewUsername))
            {
                return BadRequest(new { Message = "New username is required." });
            }

            if (_context.Users.Any(u => u.username == request.NewUsername))
            {
                return BadRequest(new { Message = "Username already exists." });
            }

            var user = _context.Users.FirstOrDefault(u => u.id == id);
            if (user == null)
            {
                return NotFound(new { Message = $"User with ID {id} not found." });
            }

            user.username = request.NewUsername;
            _context.SaveChanges();

            return Ok(new { Message = $"Username for user ID {id} updated successfully." });
        }



        [HttpGet("users")]
        [Authorize(Roles = "Admin")] // Only admins can access this endpoint
        public IActionResult GetAllUsers()
        {
            var users = _context.Users.Select(u => new
            {
                u.id,
                u.username,
                u.passwordHash,
                u.role
            }).ToList();

            return Ok(users);
        }

        [HttpDelete("users/{id}")]
        [Authorize(Roles = "Admin")] // Only admins can delete users
        public IActionResult DeleteUser(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.id == id);
            if (user == null)
            {
                return NotFound(new { Message = $"User with ID {id} not found." });
            }

            // Delete all tasks associated with the user
            var tasks = _context.Tasks.Where(t => t.userId == id).ToList();
            _context.Tasks.RemoveRange(tasks);

            // Delete the user
            _context.Users.Remove(user);
            _context.SaveChanges();

            return Ok(new { Message = $"User with ID {id} and their tasks have been deleted." });
        }


        [HttpPut("users/{id}/role")]
        [Authorize(Roles = "Admin")] // Only admins can update roles
        public IActionResult UpdateUserRole(int id, [FromBody] string newRole)
        {
            var user = _context.Users.FirstOrDefault(u => u.id == id);
            if (user == null)
            {
                return NotFound(new { Message = $"User with ID {id} not found." });
            }

            user.role = newRole;
            _context.SaveChanges();

            return Ok(new { Message = $"User with ID {id} role updated to {newRole}." });
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            return HashPassword(password) == storedHash;
        }

        private string GenerateJwtToken(int userId)
        {
            var user = _context.Users.FirstOrDefault(u => u.id == userId);
            if (user == null) throw new InvalidOperationException("User not found.");

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim("id", userId.ToString()),
                    new System.Security.Claims.Claim("role", user.role) // Include the role in the token
                }),
                Expires = DateTime.UtcNow.AddDays(7), // Token expires in 7 days
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        [HttpDelete("users/me")]
        [Authorize]
        public IActionResult DeleteOwnAccount()
        {
            // Get the user ID from the JWT claims
            var userId = int.Parse(User.Claims.First(c => c.Type == "id").Value);

            // Find the user
            var user = _context.Users.FirstOrDefault(u => u.id == userId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            // Delete all tasks (and subtasks) associated with the user
            var tasks = _context.Tasks.Where(t => t.userId == userId).ToList();
            foreach (var task in tasks)
            {
                var subtasks = _context.SubTasks.Where(st => st.taskId == task.id).ToList();
                _context.SubTasks.RemoveRange(subtasks);
            }
            _context.Tasks.RemoveRange(tasks);

            // Delete the user
            _context.Users.Remove(user);
            _context.SaveChanges();

            return Ok(new { Message = "Your account and all associated data have been deleted." });
        }

    }
}
