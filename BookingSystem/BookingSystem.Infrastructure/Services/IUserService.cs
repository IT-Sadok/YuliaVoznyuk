using BookingSystem.Domain;
using Microsoft.AspNetCore.Identity;

namespace BookingSystem.Infrastructure.Services;

public interface IUserService
{
    Task<IdentityResult> CreateUserAsync(User user, string password);
    Task AddToRoleAsync(User user, string role);
    Task<User?> FindByEmailAsync(string email);
    Task<bool> CheckPasswordAsync(User user, string password);
    Task<IList<string>> GetRolesAsync(User user);
}