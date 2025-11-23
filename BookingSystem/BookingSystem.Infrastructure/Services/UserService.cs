using BookingSystem.Domain;
using Microsoft.AspNetCore.Identity;

namespace BookingSystem.Infrastructure.Services;

public class UserService: IUserService
{
    private readonly UserManager<User> _userManager;

    public UserService(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public Task<IdentityResult> CreateUserAsync(User user, string password)
        => _userManager.CreateAsync(user, password);

    public Task AddToRoleAsync(User user, string role)
        => _userManager.AddToRoleAsync(user, role);

    public Task<User?> FindByEmailAsync(string email)
        => _userManager.FindByEmailAsync(email);

    public Task<bool> CheckPasswordAsync(User user, string password)
        => _userManager.CheckPasswordAsync(user, password);

    public Task<IList<string>> GetRolesAsync(User user)
        => _userManager.GetRolesAsync(user);
}