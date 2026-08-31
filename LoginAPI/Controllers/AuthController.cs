using Microsoft.AspNetCore.Mvc;
using LoginAPI.Models;
using LoginAPI.Data;
using BCrypt.Net;

namespace LoginAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public IActionResult Register(UserDto request)
        {
            if (_context.Users.Any(u => u.Email == request.Email))
            {
                return BadRequest("User already exists.");
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Email = request.Email,
                PasswordHash = passwordHash,
                //IsAdmin = request.Email == "admin@admin.com" // ⚠️ only this email becomes admin
            };


            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok("User registered successfully.");
        }

        [HttpPost("login")]
        public IActionResult Login(UserDto request)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == request.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return BadRequest("Invalid credentials.");
            }

            return Ok("Login successful.");
        }
        [HttpGet("all")]
        public IActionResult GetAllUsers()
        {
            var isAdmin = HttpContext.Request.Headers["admin"].ToString() == "true";

            if (!isAdmin)
                return Unauthorized("Access denied.");

            var users = _context.Users.Select(u => new
            {
                u.Id,
                u.Email,
                u.IsAdmin
            }).ToList();

            return Ok(users);
        }

        [HttpPut("{id}/email")]
        public IActionResult UpdateEmail(int id, [FromBody] UpdateEmailDto dto)
        {
            var isAdmin = Request.Headers["admin"] == "true";
            if (!isAdmin) return Unauthorized("Access denied.");

            var user = _context.Users.Find(id);
            if (user == null) return NotFound("User not found.");

            user.Email = dto.Email;
            _context.SaveChanges();
            return Ok("Email updated.");
        }

        [HttpPut("{id}/password")]
        public IActionResult UpdatePassword(int id, [FromBody] UpdatePasswordDto dto)
        {
            var isAdmin = Request.Headers["admin"] == "true";
            if (!isAdmin) return Unauthorized("Access denied.");

            var user = _context.Users.Find(id);
            if (user == null) return NotFound("User not found.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            _context.SaveChanges();
            return Ok("Password updated.");
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteUser(int id)
        {
            var isAdmin = HttpContext.Request.Headers["admin"].ToString() == "true";

            if (!isAdmin)
                return Unauthorized("Access denied.");

            var user = _context.Users.FirstOrDefault(u => u.Id == id);

            if (user == null)
                return NotFound("User not found.");

            _context.Users.Remove(user);
            _context.SaveChanges();

            return Ok("User deleted successfully.");
        }

        public class UpdateEmailDto
        {
            public string Email { get; set; } = string.Empty;
        }

        public class UpdatePasswordDto
        {
            public string Password { get; set; } = string.Empty;
        }





    }

    public class UserDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
