using Microsoft.AspNetCore.Mvc;
using LoginAPI.Models;
using LoginAPI.Data;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using BCrypt.Net;

namespace LoginAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
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
                PasswordHash = passwordHash
                // IsAdmin defaults to false. Promote users via PUT /api/Auth/{id}/role,
                // which requires an existing Admin to be authenticated.
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

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("role", user.IsAdmin ? "Admin" : "User")
            };

            var keyBytes = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);
            var signingKey = new SymmetricSecurityKey(keyBytes);
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var expiresMinutes = double.Parse(_config["Jwt:ExpiresMinutes"] ?? "60");

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
                signingCredentials: credentials
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiresAt = token.ValidTo
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("all")]
        public IActionResult GetAllUsers()
        {
            var users = _context.Users.Select(u => new
            {
                u.Id,
                u.Email,
                u.IsAdmin
            }).ToList();

            return Ok(users);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/email")]
        public IActionResult UpdateEmail(int id, [FromBody] UpdateEmailDto dto)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound("User not found.");

            user.Email = dto.Email;
            _context.SaveChanges();
            return Ok("Email updated.");
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/password")]
        public IActionResult UpdatePassword(int id, [FromBody] UpdatePasswordDto dto)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound("User not found.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            _context.SaveChanges();
            return Ok("Password updated.");
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public IActionResult DeleteUser(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);

            if (user == null)
                return NotFound("User not found.");

            _context.Users.Remove(user);
            _context.SaveChanges();

            return Ok("User deleted successfully.");
        }

        // Only an already-authenticated Admin can promote or demote another user.
        // This is the only supported way to grant IsAdmin - there is no self-service
        // or registration-time path to becoming an admin.
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/role")]
        public IActionResult UpdateRole(int id, [FromBody] UpdateRoleDto dto)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound("User not found.");

            user.IsAdmin = dto.IsAdmin;
            _context.SaveChanges();

            return Ok($"User {user.Email} IsAdmin set to {user.IsAdmin}.");
        }

        public class UpdateEmailDto
        {
            public string Email { get; set; } = string.Empty;
        }

        public class UpdatePasswordDto
        {
            public string Password { get; set; } = string.Empty;
        }

        public class UpdateRoleDto
        {
            public bool IsAdmin { get; set; }
        }





    }

    public class UserDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
