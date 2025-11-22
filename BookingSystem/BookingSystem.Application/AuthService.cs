using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BookingSystem.API;
using BookingSystem.Application.DTOs;
using BookingSystem.Domain;
using BookingSystem.Infrastructure.Services;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BookingSystem.Application;

public class AuthService : IAuthService
    {
        private readonly IUserService _userService;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly JwtSettings _jwtSettings;

        public AuthService(
            IUserService userService,
            RoleManager<IdentityRole<Guid>> roleManager,
            IOptions<JwtSettings> jwtOptions)
        {
            _userService = userService;
            _roleManager = roleManager;
            _jwtSettings = jwtOptions.Value;
        }

        public async Task<AuthResultDto> RegisterAsync(RegisterDto dto)
        {
            var user = dto.Adapt<User>();

            var result = await _userService.CreateUserAsync(user, dto.Password);
            if (!result.Succeeded)
                return AuthResultDto.Fail(result.Errors.Select(e => e.Description).ToList());

            if (!string.IsNullOrWhiteSpace(dto.Role))
            {
                var normalizedRole = dto.Role.Trim();

                if (!await _roleManager.RoleExistsAsync(normalizedRole))
                    await _roleManager.CreateAsync(new IdentityRole<Guid>(normalizedRole));

                await _userService.AddToRoleAsync(user, normalizedRole);
            }

            return AuthResultDto.Success($"User {user.Email} registered successfully with role {dto.Role}");
        }

        public async Task<AuthResultDto> LoginAsync(LoginDto dto)
        {
            var user = await _userService.FindByEmailAsync(dto.Email);
            if (user == null)
                return AuthResultDto.Fail("Invalid email or password.");

            var passwordValid = await _userService.CheckPasswordAsync(user, dto.Password);
            if (!passwordValid)
                return AuthResultDto.Fail("Invalid email or password.");

            var token = await GenerateJwtToken(user);

            return AuthResultDto.Success(token);
        }

        private async Task<string> GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? "")
            };

            var roles = await _userService.GetRolesAsync(user);
            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

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