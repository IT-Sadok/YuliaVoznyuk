using BookingSystem.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BookingSystem.API.DTOs;
using BookingSystem.Infrastructure.Services;
using Mapster;
using Microsoft.Extensions.Options;

namespace BookingSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
   public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly JwtSettings _jwtSettings;

        public AuthController(
            IUserService userService,
            RoleManager<IdentityRole<Guid>> roleManager,
            IOptions<JwtSettings> jwtOptions)
        {
            _userService = userService;
            _roleManager = roleManager;
            _jwtSettings = jwtOptions.Value;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var user = dto.Adapt<User>();

            var result = await _userService.CreateUserAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            if (!string.IsNullOrWhiteSpace(dto.Role))
            {
                var normalizedRole = dto.Role.Trim();

                // Перевіряємо, чи роль існує
                if (!await _roleManager.RoleExistsAsync(normalizedRole))
                    await _roleManager.CreateAsync(new IdentityRole<Guid>(normalizedRole));

                // Додаємо користувача до ролі
                await _userService.AddToRoleAsync(user, normalizedRole);
            }

            return Ok(new { Message = $"User {user.Email} registered successfully with role {dto.Role}" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _userService.FindByEmailAsync(dto.Email);
            if (user == null)
                return Unauthorized();

            var passwordValid = await _userService.CheckPasswordAsync(user, dto.Password);
            if (!passwordValid)
                return Unauthorized();

            var token = await GenerateJwtToken(user);

            return Ok(new { token });
        }

        private async Task<string> GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? "")
            };

            // Додаємо ролі користувача
            var roles = await _userService.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiresInMinutes);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

 
}
