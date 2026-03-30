using LocationTrackerAPI.Models;
using LocationTrackerAPI.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace LocationTrackerAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly MongoService _mongo;
        private readonly JwtService _jwt;

        public AuthController(MongoService mongo, JwtService jwt)
        {
            _mongo = mongo;
            _jwt = jwt;
        }

        // ✅ REGISTER
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User request)
        {
            // 🔍 Check if user already exists
            var existingUser = await _mongo.Users
                .Find(x => x.Username == request.Username)
                .FirstOrDefaultAsync();

            if (existingUser != null)
            {
                return BadRequest(new
                {
                    message = "User already exists"
                });
            }

            // 🔐 Hash password
            var newUser = new User
            {
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.PasswordHash),
                Role = "User" // default role
            };

            await _mongo.Users.InsertOneAsync(newUser);

            return Ok(new
            {
                message = "User registered successfully"
            });
        }

        // ✅ LOGIN
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User request)
        {
            var user = await _mongo.Users
                .Find(x => x.Username == request.Username)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new
                {
                    message = "User not found"
                });
            }

            if (!BCrypt.Net.BCrypt.Verify(request.PasswordHash, user.PasswordHash))
            {
                return Unauthorized(new
                {
                    message = "Invalid password"
                });
            }

            var token = _jwt.GenerateToken(user);

            return Ok(new
            {
                token,
                role = user.Role,
                username = user.Username
            });
        }

        // ✅ OPTIONAL: CREATE ADMIN (FOR TESTING)
        [HttpPost("create-admin")]
        public async Task<IActionResult> CreateAdmin()
        {
            var existingAdmin = await _mongo.Users
                .Find(x => x.Username == "admin")
                .FirstOrDefaultAsync();

            if (existingAdmin != null)
            {
                return BadRequest("Admin already exists");
            }

            var admin = new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = "Admin"
            };

            await _mongo.Users.InsertOneAsync(admin);

            return Ok("Admin created (username: admin, password: admin123)");
        }
    }
}